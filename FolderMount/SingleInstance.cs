using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Linq;

namespace FolderMount
{
    /// <summary>
    /// Ensures only one instance of FolderMount is running at a time.
    ///
    /// Strategy:
    ///   - A named Mutex guards the "first instance" slot.
    ///   - A NamedPipeServerStream listens for arguments from subsequent instances.
    ///   - Subsequent instances send their arguments via NamedPipeClientStream and exit.
    /// </summary>
    internal static class SingleInstance
    {
        private const string MutexName = "FolderMount_SingleInstance_Mutex_{8F2A3B4C}";
        private const string PipeName  = "FolderMount_SingleInstance_Pipe_{8F2A3B4C}";

        private static Mutex _mutex;
        private static CancellationTokenSource _cts;

        /// <summary>
        /// Fired when a second instance tries to launch and sends its arguments.
        /// </summary>
        public static event Action<string[]> ArgsReceived;

        /// <summary>
        /// Call this at application startup (before any windows are created).
        /// Returns true  → this is the first instance; proceed normally.
        /// Returns false → another instance is already running; caller should exit.
        /// </summary>
        public static bool TryClaimInstance(string[] args)
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool createdNew);

            if (!createdNew)
            {
                // Another instance already owns the mutex.
                // Send our arguments to it via Named Pipe, then exit.
                try
                {
                    using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                    {
                        client.Connect(1000); // 1-second timeout
                        using (var writer = new StreamWriter(client))
                        {
                            writer.WriteLine(string.Join("|||", args));
                            writer.Flush();
                        }
                    }
                }
                catch
                {
                    // Ignore connection errors, just exit
                }
                return false;
            }

            // We are the first instance — start a background listener thread
            _cts = new CancellationTokenSource();
            var listenerThread = new Thread(() => ListenForPipeConnections(_cts.Token))
            {
                IsBackground = true,
                Name         = "SingleInstanceListener"
            };
            listenerThread.Start();

            return true;
        }

        /// <summary>
        /// Release the mutex when the application exits.
        /// </summary>
        public static void Release()
        {
            _cts?.Cancel();         // signal the background thread to stop
            try { _mutex?.ReleaseMutex(); } catch { /* already released */ }
            _mutex?.Dispose();
            _cts?.Dispose();
            
            _mutex = null;
            _cts = null;
        }

        // ── Background listener ───────────────────────────────────────────────

        private static void ListenForPipeConnections(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        // Wait for a connection asynchronously but check cancellation periodically
                        var result = server.BeginWaitForConnection(null, null);
                        while (!result.IsCompleted)
                        {
                            if (token.IsCancellationRequested) return;
                            Thread.Sleep(50);
                        }

                        server.EndWaitForConnection(result);

                        if (token.IsCancellationRequested) return;

                        using (var reader = new StreamReader(server))
                        {
                            string message = reader.ReadLine();
                            if (message != null)
                            {
                                string[] args = string.IsNullOrEmpty(message)
                                    ? Array.Empty<string>()
                                    : message.Split(new[] { "|||" }, StringSplitOptions.None);
                                
                                // Dispatch back to the UI thread
                                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    ArgsReceived?.Invoke(args);
                                }));
                            }
                        }
                    }
                }
                catch
                {
                    // If an error occurs, wait a bit before restarting the server
                    Thread.Sleep(100);
                }
            }
        }
    }
}

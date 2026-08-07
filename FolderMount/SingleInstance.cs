using System;
using System.Threading;
using System.Windows;

namespace FolderMount
{
    /// <summary>
    /// Ensures only one instance of FolderMount is running at a time.
    ///
    /// Strategy:
    ///   - A named Mutex guards the "first instance" slot.
    ///   - A named EventWaitHandle lets the second instance signal the first
    ///     to show its main window, then the second exits immediately.
    ///
    /// No admin rights required — both objects are created in the current
    /// user session namespace.
    /// </summary>
    internal static class SingleInstance
    {
        private const string MutexName = "FolderMount_SingleInstance_Mutex_{8F2A3B4C}";
        private const string EventName = "FolderMount_ShowWindow_Event_{8F2A3B4C}";

        private static Mutex _mutex;
        private static EventWaitHandle _showEvent;
        private static Thread _listenerThread;

        /// <summary>
        /// Call this at application startup (before any windows are created).
        /// Returns true  → this is the first instance; proceed normally.
        /// Returns false → another instance is already running; caller should exit.
        /// </summary>
        public static bool TryClaimInstance()
        {
            // Create or open the named event (used for cross-instance signalling)
            _showEvent = new EventWaitHandle(
                initialState: false,
                mode:         EventResetMode.AutoReset,
                name:         EventName);

            bool createdNew;
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);

            if (!createdNew)
            {
                // Another instance already owns the mutex.
                // Signal it to show its window, then exit.
                _showEvent.Set();
                _showEvent.Dispose();
                return false;
            }

            // We are the first instance — start a background listener thread
            // that watches for show-window signals from future instances.
            _listenerThread = new Thread(ListenForShowSignal)
            {
                IsBackground = true,
                Name         = "SingleInstanceListener"
            };
            _listenerThread.Start();

            return true;
        }

        /// <summary>
        /// Release the mutex when the application exits.
        /// </summary>
        public static void Release()
        {
            _listenerThread = null; // signals the background thread to stop (app is exiting)
            _showEvent?.Set();      // unblock the wait so the thread can exit cleanly
            try { _mutex?.ReleaseMutex(); } catch { /* already released */ }
            _mutex?.Dispose();
            _showEvent?.Dispose();
        }

        // ── Background listener ───────────────────────────────────────────────

        private static void ListenForShowSignal()
        {
            while (true)
            {
                // Block until a second instance signals us (or app exits)
                bool signalled = _showEvent.WaitOne(Timeout.Infinite);

                if (!signalled || _listenerThread == null)
                    break; // app is shutting down

                // Dispatch ShowMainWindow back to the UI thread
                Application.Current?.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (Application.Current is App app)
                            app.ShowMainWindow();
                    }));
            }
        }
    }
}

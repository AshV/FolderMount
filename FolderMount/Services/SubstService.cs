using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FolderMount.Models;

namespace FolderMount.Services
{
    /// <summary>
    /// Wraps the Windows SUBST command to mount/unmount virtual drives
    /// and query currently active SUBST mappings.
    /// SUBST does NOT require elevated (admin) privileges.
    /// </summary>
    public static class SubstService
    {
        /// <summary>
        /// Mounts a folder as a virtual drive letter using SUBST.
        /// Returns (success, errorMessage).
        /// </summary>
        public static (bool Success, string Error) Mount(string driveLetter, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(driveLetter) || string.IsNullOrWhiteSpace(folderPath))
                return (false, "Drive letter and folder path are required.");

            if (!Directory.Exists(folderPath))
                return (false, $"Folder not found: {folderPath}");

            string letter = driveLetter.TrimEnd(':').ToUpper();
            return RunSubst($"{letter}: \"{folderPath}\"");
        }

        /// <summary>
        /// Removes the SUBST mapping for the given drive letter.
        /// Does not delete the saved mapping from the XML store.
        /// </summary>
        public static (bool Success, string Error) Unmount(string driveLetter)
        {
            string letter = driveLetter.TrimEnd(':').ToUpper();
            return RunSubst($"{letter}: /D");
        }

        /// <summary>
        /// Parses the output of the bare SUBST command to return all
        /// currently active virtual drive mappings as a Dictionary of letter → path.
        /// </summary>
        public static Dictionary<string, string> GetActiveMappings()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var psi = new ProcessStartInfo("subst")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    // Output format: "P:\: => D:\Projects"
                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var match = Regex.Match(line, @"^([A-Za-z]):\\:\s*=>\s*(.+)$");
                        if (match.Success)
                            result[match.Groups[1].Value.ToUpper()] = match.Groups[2].Value.Trim();
                    }
                }
            }
            catch
            {
                // If SUBST is unavailable (rare), return empty
            }
            return result;
        }

        /// <summary>
        /// Returns drive letters D–Z that are not currently in use
        /// (not present in DriveInfo.GetDrives() nor in active SUBST mappings).
        /// </summary>
        public static List<string> GetAvailableLetters()
        {
            var inUse = DriveInfo.GetDrives()
                .Select(d => d.Name.Substring(0, 1).ToUpper())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return Enumerable.Range('D', 'Z' - 'D' + 1)
                .Select(c => ((char)c).ToString())
                .Where(l => !inUse.Contains(l))
                .ToList();
        }

        /// <summary>
        /// Refreshes the IsActive flag on a collection of mappings
        /// by comparing them against the currently active SUBST output.
        /// </summary>
        public static void RefreshStatus(IEnumerable<DriveMapping> mappings)
        {
            var active = GetActiveMappings();
            foreach (var m in mappings)
            {
                m.IsActive = active.TryGetValue(m.DriveLetter, out string activePath)
                    && string.Equals(activePath, m.FolderPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Mounts all mappings in a collection silently (ignores individual failures).</summary>
        public static void MountAll(System.Collections.Generic.IEnumerable<DriveMapping> mappings)
        {
            foreach (var m in mappings)
                Mount(m.DriveLetter, m.FolderPath);
        }

        /// <summary>Unmounts all currently-active mappings in a collection silently.</summary>
        public static void UnmountAll(System.Collections.Generic.IEnumerable<DriveMapping> mappings)
        {
            var active = GetActiveMappings();
            foreach (var m in mappings)
            {
                if (active.ContainsKey(m.DriveLetter))
                    Unmount(m.DriveLetter);
            }
        }



        private static (bool Success, string Error) RunSubst(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo("subst", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string err = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    return proc.ExitCode == 0
                        ? (true, null)
                        : (false, string.IsNullOrWhiteSpace(err) ? $"SUBST exited with code {proc.ExitCode}" : err.Trim());
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FolderMount.Models;

namespace FolderMount.Services
{
    /// <summary>
    /// Uses native Windows Win32 APIs to mount/unmount virtual drives.
    /// This is significantly faster than launching subst.exe processes.
    /// </summary>
    public static class SubstService
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool DefineDosDevice(uint dwFlags, string lpDeviceName, string lpTargetPath);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice(string lpDeviceName, [Out] char[] lpTargetPath, uint ucchMax);

        private const uint DDD_RAW_TARGET_PATH = 0x00000001;
        private const uint DDD_REMOVE_DEFINITION = 0x00000002;

        /// <summary>
        /// Mounts a folder as a virtual drive letter using DefineDosDevice.
        /// Returns (success, errorMessage).
        /// </summary>
        public static (bool Success, string Error) Mount(string driveLetter, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(driveLetter) || string.IsNullOrWhiteSpace(folderPath))
                return (false, "Drive letter and folder path are required.");

            if (!Directory.Exists(folderPath))
                return (false, $"Folder not found: {folderPath}");

            string letter = driveLetter.TrimEnd(':').ToUpper();

            // Check if it's already mounted correctly
            var active = GetActiveMappings();
            if (active.TryGetValue(letter, out string existingPath)
                && string.Equals(existingPath, folderPath, StringComparison.OrdinalIgnoreCase))
            {
                return (true, null);
            }

            // Check if letter is in use by a real drive or another mapping
            bool letterInUse = DriveInfo.GetDrives()
                .Any(d => d.Name.StartsWith(letter + ":", StringComparison.OrdinalIgnoreCase));

            if (letterInUse && !active.ContainsKey(letter))
            {
                return (false,
                    $"Drive {letter}: is already in use by another drive or mapping.\n" +
                    $"Disconnect the existing drive first, or choose a different letter.");
            }

            string targetPath = @"\??\" + folderPath;
            bool result = DefineDosDevice(DDD_RAW_TARGET_PATH, letter + ":", targetPath);

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                return (false, $"Failed to mount. Error code: {error}");
            }

            return (true, null);
        }

        /// <summary>
        /// Removes the mapping for the given drive letter.
        /// </summary>
        public static (bool Success, string Error) Unmount(string driveLetter)
        {
            string letter = driveLetter.TrimEnd(':').ToUpper();
            
            bool result = DefineDosDevice(DDD_REMOVE_DEFINITION, letter + ":", null);
            
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                return (false, $"Failed to unmount. Error code: {error}");
            }
            return (true, null);
        }

        /// <summary>
        /// Retrieves all currently active virtual drive mappings.
        /// </summary>
        public static Dictionary<string, string> GetActiveMappings()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (char c = 'A'; c <= 'Z'; c++)
            {
                string deviceName = c + ":";
                char[] targetPath = new char[1024];
                uint ret = QueryDosDevice(deviceName, targetPath, (uint)targetPath.Length);

                if (ret != 0)
                {
                    string path = new string(targetPath, 0, (int)ret - 2); // Remove trailing \0\0
                    
                    if (path.StartsWith(@"\??\"))
                    {
                        result[c.ToString()] = path.Substring(4);
                    }
                }
            }

            return result;
        }

        public static List<string> GetAvailableLetters(IEnumerable<string> excludeLetters = null)
        {
            var inUse = DriveInfo.GetDrives()
                .Select(d => d.Name.Substring(0, 1).ToUpper())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (excludeLetters != null)
                foreach (var l in excludeLetters)
                    inUse.Add(l.TrimEnd(':').ToUpper());

            return Enumerable.Range('A', 26)
                .Select(c => ((char)c).ToString())
                .Where(l => !inUse.Contains(l))
                .ToList();
        }

        public static void RefreshStatus(IEnumerable<DriveMapping> mappings)
        {
            var active = GetActiveMappings();
            foreach (var m in mappings)
            {
                m.IsActive = active.TryGetValue(m.DriveLetter, out string activePath)
                    && string.Equals(activePath, m.FolderPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static (int Mounted, List<(string Letter, string Error)> Failures)
            MountAll(IEnumerable<DriveMapping> mappings)
        {
            int mounted = 0;
            var failures = new List<(string, string)>();
            foreach (var m in mappings)
            {
                var (ok, err) = Mount(m.DriveLetter, m.FolderPath);
                if (ok) mounted++;
                else    failures.Add((m.DisplayLetter, err));
            }
            return (mounted, failures);
        }

        public static void UnmountAll(IEnumerable<DriveMapping> mappings)
        {
            var active = GetActiveMappings();
            foreach (var m in mappings)
            {
                if (active.ContainsKey(m.DriveLetter))
                    Unmount(m.DriveLetter);
            }
        }
    }
}

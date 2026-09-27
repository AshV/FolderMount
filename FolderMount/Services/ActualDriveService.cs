using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FolderMount.Models;

namespace FolderMount.Services
{
    /// <summary>
    /// Service for discovering and querying actual (physical / logical) drives on the system,
    /// explicitly excluding virtual drives created via SUBST / DefineDosDevice.
    /// </summary>
    public static class ActualDriveService
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice(string lpDeviceName, [Out] char[] lpTargetPath, uint ucchMax);

        /// <summary>
        /// Retrieves all real physical and logical drives (Fixed, Removable, etc.)
        /// excluding virtual drives created by SUBST or DefineDosDevice.
        /// </summary>
        public static List<ActualDrive> GetActualDrives()
        {
            var result = new List<ActualDrive>();
            var drives = DriveInfo.GetDrives();

            foreach (var d in drives)
            {
                if (!d.IsReady) continue;
                if (d.DriveType == DriveType.Network) continue;

                string letter = d.Name.Substring(0, 1).ToUpper();

                // Check QueryDosDevice to identify and filter out SUBST / virtual drives
                char[] targetPath = new char[1024];
                uint ret = QueryDosDevice(letter + ":", targetPath, (uint)targetPath.Length);
                if (ret != 0)
                {
                    string path = new string(targetPath, 0, (int)ret - 2);
                    // Virtual DOS device drives start with \??\
                    if (path.StartsWith(@"\??\"))
                    {
                        continue;
                    }
                }

                string volLabel = string.Empty;
                try
                {
                    volLabel = d.VolumeLabel ?? string.Empty;
                }
                catch { }

                string driveLabel = DriveLabelService.GetDriveLabel(letter);

                result.Add(new ActualDrive
                {
                    DriveLetter = letter,
                    VolumeLabel = volLabel,
                    DriveLabel = driveLabel,
                    DriveType = d.DriveType,
                    FileSystem = d.DriveFormat,
                    TotalSizeBytes = d.TotalSize,
                    FreeSizeBytes = d.TotalFreeSpace
                });
            }

            return result.OrderBy(x => x.DriveLetter).ToList();
        }
    }
}

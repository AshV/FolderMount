using System;
using System.IO;

namespace FolderMount.Services
{
    public static class AppPaths
    {
        public static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderMount");
    }
}

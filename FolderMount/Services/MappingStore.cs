using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FolderMount.Models;

namespace FolderMount.Services
{
    /// <summary>
    /// Persists drive mappings to %APPDATA%\FolderMount\mappings.xml.
    /// Uses XDocument (System.Xml.Linq) — no third-party dependencies.
    /// </summary>
    public static class MappingStore
    {
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderMount");

        public static string MappingsFilePath => Path.Combine(AppDataDir, "mappings.xml");

        // ── Load ───────────────────────────────────────────────────────────────

        /// <summary>Loads saved mappings from XML. Returns empty list if file doesn't exist.</summary>
        public static List<DriveMapping> Load()
        {
            EnsureDir();
            if (!File.Exists(MappingsFilePath))
                return new List<DriveMapping>();

            try
            {
                return Parse(XDocument.Load(MappingsFilePath));
            }
            catch
            {
                return new List<DriveMapping>();
            }
        }

        // ── Save ───────────────────────────────────────────────────────────────

        /// <summary>Saves the full mapping collection to XML, overwriting any previous file.</summary>
        public static void Save(IEnumerable<DriveMapping> mappings)
        {
            EnsureDir();
            BuildDocument(mappings).Save(MappingsFilePath);
        }

        // ── Export ─────────────────────────────────────────────────────────────

        /// <summary>Exports mappings to a user-chosen path (e.g. Desktop).</summary>
        public static void Export(string destinationPath, IEnumerable<DriveMapping> mappings)
            => BuildDocument(mappings).Save(destinationPath);

        // ── Import ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Imports mappings from an XML file without merging.
        /// </summary>
        public static List<DriveMapping> Import(string sourcePath)
        {
            return Parse(XDocument.Load(sourcePath));
        }

        /// <summary>
        /// Imports mappings from an XML file and merges with existing.
        /// Imported entries override existing entries with the same drive letter.
        /// Returns the merged list.
        /// </summary>
        public static List<DriveMapping> Import(string sourcePath, IEnumerable<DriveMapping> existing)
        {
            var imported = Parse(XDocument.Load(sourcePath));
            var merged = existing.ToDictionary(m => m.DriveLetter.ToUpper(), StringComparer.OrdinalIgnoreCase);
            foreach (var m in imported)
                merged[m.DriveLetter.ToUpper()] = m;
            return merged.Values.OrderBy(m => m.DriveLetter).ToList();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static void EnsureDir()
        {
            if (!Directory.Exists(AppDataDir))
                Directory.CreateDirectory(AppDataDir);
        }

        private static List<DriveMapping> Parse(XDocument doc)
        {
            return doc.Root?
                .Elements("Mapping")
                .Select(e => new DriveMapping
                {
                    DriveLetter = (string)e.Attribute("letter"),
                    FolderPath  = (string)e.Attribute("path"),
                    Label       = (string)e.Attribute("label") ?? string.Empty,
                    MountOnLoad = e.Attribute("mountOnLoad") != null ? (bool)e.Attribute("mountOnLoad") : true
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.DriveLetter) && !string.IsNullOrWhiteSpace(m.FolderPath))
                .ToList()
                ?? new List<DriveMapping>();
        }

        private static XDocument BuildDocument(IEnumerable<DriveMapping> mappings)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("FolderMounts",
                    new XComment(" FolderMount mappings — edit with care "),
                    mappings.Select(m =>
                        new XElement("Mapping",
                            new XAttribute("letter", m.DriveLetter),
                            new XAttribute("path",   m.FolderPath),
                            new XAttribute("label",  m.Label ?? string.Empty),
                            new XAttribute("mountOnLoad", m.MountOnLoad)
                        )
                    )
                )
            );
        }
    }
}

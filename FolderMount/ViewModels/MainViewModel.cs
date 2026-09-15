using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FolderMount.Models;
using FolderMount.Services;
using Microsoft.Win32;

namespace FolderMount.ViewModels
{
    /// <summary>
    /// Main ViewModel — owns the mapping collection and all toolbar commands.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ── State ──────────────────────────────────────────────────────────────

        public ObservableCollection<DriveMapping> Mappings { get; } = new ObservableCollection<DriveMapping>();

        private DriveMapping _selectedMapping;
        public DriveMapping SelectedMapping
        {
            get => _selectedMapping;
            set { _selectedMapping = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int ActiveCount => Mappings.Count(m => m.IsActive);
        public int TotalCount  => Mappings.Count;

        // ── Commands ───────────────────────────────────────────────────────────

        public ICommand AddCommand          { get; }
        public ICommand EditCommand         { get; }
        public ICommand RemoveCommand       { get; }
        public ICommand MountCommand        { get; }
        public ICommand UnmountCommand      { get; }
        public ICommand MountAllCommand     { get; }
        public ICommand UnmountAllCommand   { get; }
        public ICommand OpenExplorerCommand { get; }
        public ICommand ExportCommand       { get; }
        public ICommand ImportCommand       { get; }
        public ICommand RefreshCommand      { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public MainViewModel()
        {
            AddCommand          = new RelayCommand(DoAdd);
            EditCommand         = new RelayCommand(DoEdit);
            RemoveCommand       = new RelayCommand(DoRemove);
            MountCommand        = new RelayCommand(DoMount);
            UnmountCommand      = new RelayCommand(DoUnmount);
            MountAllCommand     = new RelayCommand(DoMountAll,   () => Mappings.Any(m => !m.IsActive));
            UnmountAllCommand   = new RelayCommand(DoUnmountAll, () => Mappings.Any(m => m.IsActive));
            OpenExplorerCommand = new RelayCommand(DoOpenExplorer);
            ExportCommand       = new RelayCommand(DoExport, () => Mappings.Count > 0);
            ImportCommand       = new RelayCommand(DoImport);
            RefreshCommand      = new RelayCommand(DoRefresh);

            LoadMappings();
        }

        // ── Load ───────────────────────────────────────────────────────────────

        public void LoadMappings()
        {
            Mappings.Clear();
            foreach (var m in MappingStore.Load())
                Mappings.Add(m);
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            SubstService.RefreshStatus(Mappings);
            NotifyCounts();
            StatusMessage = $"Active: {ActiveCount} / {TotalCount}";
        }

        // ── Command implementations ────────────────────────────────────────────

        private void DoAdd()
        {
            var usedLetters = Mappings.Select(m => m.DriveLetter);
            var dlg = new AddEditDialog(null, usedLetters) { Owner = Application.Current.MainWindow };

            if (dlg.ShowDialog() != true) return;

            var m = dlg.Result;
            if (Mappings.Any(x => x.DriveLetter.Equals(m.DriveLetter, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Drive {m.DisplayLetter} is already in the list.", "Duplicate Letter",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Mappings.Add(m);
            SaveAll();
            RefreshStatus();
            StatusMessage = $"Added {m.DisplayLetter}";
        }

        private void DoEdit(object param)
        {
            var mapping = ResolveMapping(param);
            if (mapping == null) return;

            var usedLetters = Mappings.Where(m => m != mapping).Select(m => m.DriveLetter);
            var dlg = new AddEditDialog(mapping.Clone(), usedLetters) { Owner = Application.Current.MainWindow };

            if (dlg.ShowDialog() != true) return;

            var newM = dlg.Result;
            var idx  = Mappings.IndexOf(mapping);
            if (idx < 0) return;

            Mappings[idx] = newM;
            if (mapping.IsActive)
                SubstService.Unmount(mapping.DriveLetter);

            SaveAll();
            RefreshStatus();
            StatusMessage = $"Updated {newM.DisplayLetter}";
        }

        private void DoRemove(object param)
        {
            var mapping = ResolveMapping(param);
            if (mapping == null) return;

            var ans = MessageBox.Show(
                $"Remove mapping for {mapping.DisplayLetter}?\n\nThe virtual drive will be disconnected if currently active.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ans != MessageBoxResult.Yes) return;

            if (mapping.IsActive)
                SubstService.Unmount(mapping.DriveLetter);

            Mappings.Remove(mapping);
            SaveAll();
            RefreshStatus();
            StatusMessage = "Mapping removed.";
        }

        private void DoMount(object param)
        {
            var mapping = ResolveMapping(param);
            if (mapping == null) return;

            var (ok, err) = SubstService.Mount(mapping.DriveLetter, mapping.FolderPath);
            if (ok)
            {
                mapping.IsActive = true;
                mapping.MountOnLoad = true;
                SaveAll();
                NotifyCounts();
                StatusMessage = $"{mapping.DisplayLetter} mounted.";
            }
            else
            {
                ShowError($"Could not mount {mapping.DisplayLetter}:\n{err}");
            }
        }

        private void DoUnmount(object param)
        {
            var mapping = ResolveMapping(param);
            if (mapping == null) return;

            var (ok, err) = SubstService.Unmount(mapping.DriveLetter);
            if (ok)
            {
                mapping.IsActive = false;
                mapping.MountOnLoad = false;
                SaveAll();
                NotifyCounts();
                StatusMessage = $"{mapping.DisplayLetter} disconnected.";
            }
            else
            {
                ShowError($"Could not disconnect {mapping.DisplayLetter}:\n{err}");
            }
        }

        public void DoMountAll()
        {
            var inactive = Mappings.Where(x => !x.IsActive).ToList();
            if (inactive.Count == 0)
            {
                StatusMessage = "All drives are already mounted.";
                return;
            }

            var (mounted, failures) = SubstService.MountAll(inactive);

            // Update model state for successfully mounted drives
            foreach (var m in inactive.Where(m => !failures.Any(f => f.Letter == m.DisplayLetter)))
            {
                m.IsActive = true;
                m.MountOnLoad = true;
            }

            NotifyCounts();
            if (mounted > 0) SaveAll();

            if (failures.Count > 0)
            {
                var errorLines = string.Join("\n", failures.Select(f => $"  {f.Letter}  →  {f.Error}"));
                StatusMessage = $"Mounted {mounted} drive(s). {failures.Count} failed.";
                ShowError($"Mounted {mounted} of {inactive.Count} drive(s).\n\nThe following could not be mounted:\n{errorLines}");
            }
            else
            {
                StatusMessage = $"All {mounted} drive(s) mounted successfully.";
            }
        }

        public void DoUnmountAll()
        {
            SubstService.UnmountAll(Mappings.Where(x => x.IsActive));

            foreach (var m in Mappings)
            {
                m.IsActive = false;
                m.MountOnLoad = false;
            }

            SaveAll();
            NotifyCounts();
            StatusMessage = "All drives disconnected.";
        }

        private void DoOpenExplorer(object param)
        {
            var mapping = ResolveMapping(param);
            if (mapping?.IsActive == true)
                Process.Start("explorer.exe", $"{mapping.DisplayLetter}\\");
        }

        private void DoExport()
        {
            var dlg = new SaveFileDialog
            {
                Title      = "Export Mappings",
                Filter     = "XML Files (*.xml)|*.xml",
                FileName   = "FolderMount_mappings.xml",
                DefaultExt = "xml"
            };
            if (dlg.ShowDialog() == true)
            {
                MappingStore.Export(dlg.FileName, Mappings);
                StatusMessage = $"Exported to {Path.GetFileName(dlg.FileName)}";
            }
        }

        private void DoImport()
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Import Mappings",
                Filter = "XML Files (*.xml)|*.xml"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var merged = MappingStore.Import(dlg.FileName, Mappings);
                Mappings.Clear();
                foreach (var m in merged) Mappings.Add(m);
                SaveAll();
                RefreshStatus();
                StatusMessage = $"Imported {merged.Count} mapping(s).";
            }
            catch (Exception ex)
            {
                ShowError($"Import failed:\n{ex.Message}");
            }
        }

        private void DoRefresh() => RefreshStatus();

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SaveAll() => MappingStore.Save(Mappings);

        /// <summary>
        /// Resolves the target mapping from a command parameter, falling back to SelectedMapping.
        /// Centralises the "param as DriveMapping ?? SelectedMapping" pattern used by all row commands.
        /// </summary>
        private DriveMapping ResolveMapping(object param) => param as DriveMapping ?? SelectedMapping;

        private void NotifyCounts()
        {
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        private static void ShowError(string msg)
            => MessageBox.Show(msg, "FolderMount Error", MessageBoxButton.OK, MessageBoxImage.Error);

        // ── INotifyPropertyChanged ─────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

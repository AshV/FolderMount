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

        public ICommand AddCommand        { get; }
        public ICommand EditCommand       { get; }
        public ICommand RemoveCommand     { get; }
        public ICommand MountCommand      { get; }
        public ICommand UnmountCommand    { get; }
        public ICommand MountAllCommand   { get; }
        public ICommand UnmountAllCommand { get; }
        public ICommand OpenExplorerCommand { get; }
        public ICommand ExportCommand     { get; }
        public ICommand ImportCommand     { get; }
        public ICommand RefreshCommand    { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public MainViewModel()
        {
            AddCommand          = new RelayCommand(DoAdd);
            EditCommand         = new RelayCommand(DoEdit,   () => SelectedMapping != null);
            RemoveCommand       = new RelayCommand(DoRemove, () => SelectedMapping != null);
            MountCommand        = new RelayCommand(DoMount,      () => SelectedMapping != null && !SelectedMapping.IsActive);
            UnmountCommand      = new RelayCommand(DoUnmount,    () => SelectedMapping != null && SelectedMapping.IsActive);
            MountAllCommand     = new RelayCommand(DoMountAll,   () => Mappings.Any(m => !m.IsActive));
            UnmountAllCommand   = new RelayCommand(DoUnmountAll, () => Mappings.Any(m => m.IsActive));
            OpenExplorerCommand = new RelayCommand(DoOpenExplorer, () => SelectedMapping != null && SelectedMapping.IsActive);
            ExportCommand       = new RelayCommand(DoExport, () => Mappings.Count > 0);
            ImportCommand       = new RelayCommand(DoImport);
            RefreshCommand      = new RelayCommand(DoRefresh);

            LoadMappings();
        }

        // ── Load ───────────────────────────────────────────────────────────────

        public void LoadMappings()
        {
            Mappings.Clear();
            var saved = MappingStore.Load();
            foreach (var m in saved)
                Mappings.Add(m);
            RefreshStatus();
            NotifyCounts();
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
            var dlg = new AddEditDialog(null);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
            {
                var m = dlg.Result;
                // Conflict check
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
        }

        private void DoEdit()
        {
            if (SelectedMapping == null) return;
            var dlg = new AddEditDialog(SelectedMapping.Clone());
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
            {
                var edited = dlg.Result;
                // If drive letter changed, check for conflicts
                if (!edited.DriveLetter.Equals(SelectedMapping.DriveLetter, StringComparison.OrdinalIgnoreCase)
                    && Mappings.Any(x => x != SelectedMapping && x.DriveLetter.Equals(edited.DriveLetter, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Drive {edited.DisplayLetter} is already in the list.", "Duplicate Letter",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // If currently active and something changed, unmount first
                if (SelectedMapping.IsActive)
                    SubstService.Unmount(SelectedMapping.DriveLetter);

                SelectedMapping.DriveLetter = edited.DriveLetter;
                SelectedMapping.FolderPath  = edited.FolderPath;
                SelectedMapping.Label       = edited.Label;
                SaveAll();
                RefreshStatus();
                StatusMessage = $"Updated {SelectedMapping.DisplayLetter}";
            }
        }

        private void DoRemove()
        {
            if (SelectedMapping == null) return;
            var ans = MessageBox.Show(
                $"Remove mapping for {SelectedMapping.DisplayLetter}?\n\nThe virtual drive will be disconnected if currently active.",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ans != MessageBoxResult.Yes) return;

            if (SelectedMapping.IsActive)
                SubstService.Unmount(SelectedMapping.DriveLetter);

            Mappings.Remove(SelectedMapping);
            SaveAll();
            RefreshStatus();
            StatusMessage = "Mapping removed.";
        }

        private void DoMount()
        {
            if (SelectedMapping == null) return;
            var (ok, err) = SubstService.Mount(SelectedMapping.DriveLetter, SelectedMapping.FolderPath);
            if (ok)
            {
                SelectedMapping.IsActive = true;
                NotifyCounts();
                StatusMessage = $"{SelectedMapping.DisplayLetter} mounted.";
            }
            else
            {
                ShowError($"Could not mount {SelectedMapping.DisplayLetter}:\n{err}");
            }
        }

        private void DoUnmount()
        {
            if (SelectedMapping == null) return;
            var (ok, err) = SubstService.Unmount(SelectedMapping.DriveLetter);
            if (ok)
            {
                SelectedMapping.IsActive = false;
                NotifyCounts();
                StatusMessage = $"{SelectedMapping.DisplayLetter} disconnected.";
            }
            else
            {
                ShowError($"Could not disconnect {SelectedMapping.DisplayLetter}:\n{err}");
            }
        }

        private void DoMountAll()
        {
            int mounted = 0, failed = 0;
            foreach (var m in Mappings.Where(x => !x.IsActive))
            {
                var (ok, _) = SubstService.Mount(m.DriveLetter, m.FolderPath);
                if (ok) { m.IsActive = true; mounted++; }
                else failed++;
            }
            NotifyCounts();
            StatusMessage = failed > 0
                ? $"Mounted {mounted}, {failed} failed — check folder paths."
                : $"All {mounted} drive(s) mounted.";
        }

        private void DoUnmountAll()
        {
            foreach (var m in Mappings.Where(x => x.IsActive))
            {
                SubstService.Unmount(m.DriveLetter);
                m.IsActive = false;
            }
            NotifyCounts();
            StatusMessage = "All drives disconnected.";
        }

        private void DoOpenExplorer()
        {
            if (SelectedMapping?.IsActive == true)
                Process.Start("explorer.exe", $"{SelectedMapping.DisplayLetter}\\");
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
            if (dlg.ShowDialog() == true)
            {
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
        }

        private void DoRefresh() => RefreshStatus();

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SaveAll() => MappingStore.Save(Mappings);

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

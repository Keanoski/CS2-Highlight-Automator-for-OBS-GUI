using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_auto_highlights_gui;
using HighlightReel.Models;
using HighlightReel.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HighlightReelGUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly OBSController _obsController = new();
        private const string SettingsFilePath = "appsettings.json";

        // --- Properties bound to the UI ---
        [ObservableProperty]
        private string _cs2Path = "";

        [ObservableProperty]
        private string _demoPath = "";

        [ObservableProperty]
        private string _obsIp = "";

        [ObservableProperty]
        private string _obsPort = "";

        [ObservableProperty]
        private string _obsPassword = "";

        public ObservableCollection<Highlight> Highlights { get; set; } = new();

        public MainViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            AppSettings settings;
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                settings = new AppSettings();
            }

            Cs2Path = settings.Cs2Path;
            DemoPath = settings.DemoPath;
            ObsIp = settings.ObsIp;
            ObsPort = settings.ObsPort;
            ObsPassword = settings.ObsPassword;
        }


        public void SaveSettings()
        {
            var settings = new AppSettings
            {
                Cs2Path = this.Cs2Path,
                DemoPath = this.DemoPath,
                ObsIp = this.ObsIp,
                ObsPort = this.ObsPort,
                ObsPassword = this.ObsPassword
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }

        // --- Commands ---
        [RelayCommand]
        private void BrowseForDemoFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CS2 Demo Files (*.dem)|*.dem|All files (*.*)|*.*",
                Title = "Select a Demo File"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                DemoPath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseForCs2()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CS2 Executable (cs2.exe)|cs2.exe",
                Title = "Select cs2.exe"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Cs2Path = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private async Task FindHighlightsAsync()
        {
            if (!File.Exists(DemoPath))
            {
                MessageBox.Show("Demo file path is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var foundHighlights = await HighlightFinder.FindHighlightsAsync(DemoPath);

                Highlights.Clear();
                foreach (var highlight in foundHighlights)
                {
                    Highlights.Add(highlight);
                }
                MessageBox.Show($"Found {Highlights.Count} highlights.", "Analysis Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while analyzing the demo: {ex.Message}", "Analysis Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LaunchAndRecordAsync()
        {
            if (!Highlights.Any())
            {
                MessageBox.Show("No highlights found or loaded. Please find highlights first.", "No Highlights", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cs2Process = (Process?)null;

            try
            {
                var obsResult = await _obsController.ConnectAsync(ObsIp, ObsPort, ObsPassword);
                if (!obsResult.IsConnected)
                {
                    MessageBox.Show($"Could not connect to OBS: {obsResult.ErrorMessage}", "OBS Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SetupScriptGenerator.Generate(Cs2Path);

                var cs2Result = CS2Launcher.Launch(Cs2Path, DemoPath);
                if (cs2Result.Process == null)
                {
                    MessageBox.Show(cs2Result.ErrorMessage, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _obsController.Disconnect();
                    return;
                }
                cs2Process = cs2Result.Process;

                await Task.Delay(25000);

                await _obsController.RecordHighlightsAsync(cs2Process, Highlights.ToList());

                MessageBox.Show("Recording complete!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _obsController.Disconnect();
                if (cs2Process != null && !cs2Process.HasExited)
                {
                    cs2Process.Kill();
                }
            }
        }
    }
}
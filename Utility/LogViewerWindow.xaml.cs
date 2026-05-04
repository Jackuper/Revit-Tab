using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Revit_Tab.Utility
{
    public partial class LogViewerWindow : Window
    {
        private FileSystemWatcher _fileWatcher;
        private string _currentLogFile;
        private long _lastFilePosition;
        private DispatcherTimer _refreshTimer;
        private string _currentFilter = "All";
        private bool _isLoaded = false;

        public LogViewerWindow()
        {
            InitializeComponent();
            Loaded += LogViewerWindow_Loaded;
            Closing += LogViewerWindow_Closing;
        }

        private void LogViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get current log file
            _currentLogFile = Path.Combine(ErrorHandler.GetLogDirectory(), $"RevitTab_{DateTime.Now:yyyy-MM-dd}.log");
            LogFileTextBlock.Text = $"Monitoring: {Path.GetFileName(_currentLogFile)}";

            // Load existing log content
            LoadLogContent();

            // Set up file watcher for real-time updates
            SetupFileWatcher();

            // Set up refresh timer as backup
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            UpdateStatus($"Monitoring log file - {GetFileSize(_currentLogFile)}");

            // Mark as fully loaded
            _isLoaded = true;
        }

        private void LogViewerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up watchers and timers
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
            }

            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
            }
        }

        private void SetupFileWatcher()
        {
            try
            {
                string logDirectory = ErrorHandler.GetLogDirectory();

                _fileWatcher = new FileSystemWatcher(logDirectory)
                {
                    Filter = "*.log",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += FileWatcher_Changed;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error setting up file watcher: {ex.Message}");
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Only update if it's today's log file
            if (e.FullPath == _currentLogFile)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadNewLogEntries();
                }));
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadNewLogEntries();
            UpdateStatus($"Last updated: {DateTime.Now:HH:mm:ss} - {GetFileSize(_currentLogFile)}");
        }

        private void LoadLogContent()
        {
            try
            {
                if (File.Exists(_currentLogFile))
                {
                    string content = File.ReadAllText(_currentLogFile, Encoding.UTF8);
                    _lastFilePosition = new FileInfo(_currentLogFile).Length;

                    ApplyFilter(content);

                    if (AutoScrollCheckBox.IsChecked == true)
                    {
                        LogTextBox.ScrollToEnd();
                    }
                }
                else
                {
                    LogTextBox.Text = "No log entries for today yet. Run a command to generate logs.";
                    _lastFilePosition = 0;
                }
            }
            catch (Exception ex)
            {
                LogTextBox.Text = $"Error loading log file: {ex.Message}";
            }
        }

        private void LoadNewLogEntries()
        {
            try
            {
                if (!File.Exists(_currentLogFile))
                    return;

                var fileInfo = new FileInfo(_currentLogFile);
                long currentLength = fileInfo.Length;

                // Only read new content if file has grown
                if (currentLength > _lastFilePosition)
                {
                    using (FileStream fs = new FileStream(_currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(_lastFilePosition, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string newContent = reader.ReadToEnd();
                            if (!string.IsNullOrEmpty(newContent))
                            {
                                AppendFilteredContent(newContent);

                                if (AutoScrollCheckBox.IsChecked == true)
                                {
                                    LogTextBox.ScrollToEnd();
                                }
                            }
                        }
                    }

                    _lastFilePosition = currentLength;
                }
            }
            catch (IOException)
            {
                // File might be locked, skip this update
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error reading log updates: {ex.Message}");
            }
        }

        private void ApplyFilter(string content)
        {
            if (_currentFilter == "All")
            {
                LogTextBox.Text = content;
            }
            else
            {
                var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var filteredLines = lines.Where(line => line.Contains($"] {_currentFilter}:"));
                LogTextBox.Text = string.Join(Environment.NewLine, filteredLines);
            }
        }

        private void AppendFilteredContent(string newContent)
        {
            if (_currentFilter == "All")
            {
                LogTextBox.AppendText(newContent);
            }
            else
            {
                var lines = newContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var filteredLines = lines.Where(line => line.Contains($"] {_currentFilter}:"));
                string filteredContent = string.Join(Environment.NewLine, filteredLines);

                if (!string.IsNullOrEmpty(filteredContent))
                {
                    LogTextBox.AppendText(filteredContent);
                }
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't process selection changes during initialization
            if (!_isLoaded)
                return;

            if (FilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentFilter = selectedItem.Content.ToString();
                _lastFilePosition = 0; // Reset to reload entire file with new filter
                LoadLogContent();
                UpdateStatus($"Filter changed to: {_currentFilter}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _lastFilePosition = 0; // Force full reload
            LoadLogContent();
            UpdateStatus("Log refreshed");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            _lastFilePosition = File.Exists(_currentLogFile) ? new FileInfo(_currentLogFile).Length : 0;
            UpdateStatus("Display cleared (file not modified)");
        }

        private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", ErrorHandler.GetLogDirectory());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open log folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenScreenshotsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string screenshotDir = ErrorHandler.GetScreenshotDirectory();
                if (Directory.Exists(screenshotDir))
                {
                    Process.Start("explorer.exe", screenshotDir);
                }
                else
                {
                    MessageBox.Show("No screenshots folder exists yet. Screenshots are created automatically when errors occur.",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open screenshots folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private string GetFileSize(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    long bytes = new FileInfo(filePath).Length;
                    if (bytes < 1024)
                        return $"{bytes} bytes";
                    else if (bytes < 1024 * 1024)
                        return $"{bytes / 1024:F1} KB";
                    else
                        return $"{bytes / (1024 * 1024):F1} MB";
                }
            }
            catch { }

            return "0 bytes";
        }
    }
}

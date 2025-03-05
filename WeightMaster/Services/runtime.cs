using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WeightMaster.Services
{
    internal class Runtime
    {
        private string filePath;
        private FileSystemWatcher fileWatcher;
        private DispatcherTimer readTimer;
        private MainWindow window;

        public Runtime(MainWindow win, string file)
        {
            this.window = win;
            this.filePath = file;
        }

        public void SetFilePath(string path)
        {
            filePath = path;
        }

        public void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                StartFileWatcher();
                StartTimer();
            }
        }

        public void StartFileWatcher()
        {
            fileWatcher?.Dispose();
            fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(filePath),
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.EnableRaisingEvents = true;
            ReadFile(); // Initial read
        }

        public void StartTimer()
        {
            if (readTimer == null)
            {
                readTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                readTimer.Tick += (s, e) => ReadFile();
            }
            readTimer.Start();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ReadFile);
        }

        private void ReadFile()
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<WeightData>(json, options);
                    if (data != null)
                    {
                        //System.Diagnostics.Debug.WriteLine($"> Value: {data.Value}, Stable: {data.Stable}");
                        //window.valueLabel.Text = data.Value;
                        //if (data.Stable.Equals("true")) {
                        //    window.statusLabelS.Foreground = new SolidColorBrush(Colors.Green);
                        //    window.statusLabelS.Text = "STABLE";
                            
                        //}
                        //else
                        //{
                        //    window.statusLabelS.Foreground = new SolidColorBrush(Colors.Red);
                        //    window.statusLabelS.Text = "UNSTABLE";

                        //}

                    }
                }
            }
            catch (IOException)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Threading.Thread.Sleep(100);
                    ReadFile();
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
            }
        }

        public class WeightData
        {
            public string Value { get; set; }
            public string Stable { get; set; }
        }

        public void OnWindowClosed()
        {
            fileWatcher?.Dispose();
            readTimer?.Stop();
        }
    }
}

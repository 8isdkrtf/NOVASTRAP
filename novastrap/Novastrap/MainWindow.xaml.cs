using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Novastrap
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<FFlagEntry> _flags = new ObservableCollection<FFlagEntry>();
        private ObservableCollection<FFlagEntry> _filteredFlags = new ObservableCollection<FFlagEntry>();
        private string _settingsPath = string.Empty;
        private string _robloxPath = string.Empty;
        private AppSettings _settings = new AppSettings();
        private string _searchText = string.Empty;
        private string _lang = "ua";

        private Dictionary<string, Dictionary<string, string>> _loc = new Dictionary<string, Dictionary<string, string>>();

        public MainWindow()
        {
            InitializeComponent();
            
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsPath = Path.Combine(appData, "NovaStrap", "settings.json");
            Directory.CreateDirectory(Path.Combine(appData, "NovaStrap"));
            
            InitLocalization();
            LoadLanguage();
            LoadSettings();
            FindRobloxAutomatically();
            LoadFlags();
            UpdateStatus();
            
            FlagsGrid.ItemsSource = _filteredFlags;
            
            CategoryCombo.Items.Clear();
            CategoryCombo.Items.Add("📁 Усе");
            CategoryCombo.Items.Add("🎨 Graphics");
            CategoryCombo.Items.Add("⚡ Performance");
            CategoryCombo.Items.Add("🌐 Network");
            CategoryCombo.Items.Add("🐛 Debug");
            CategoryCombo.Items.Add("👤 UGC");
            CategoryCombo.Items.Add("📁 Iнше");
            CategoryCombo.SelectedIndex = 0;
            
            ApplyLanguage();
        }

        private void InitLocalization()
        {
            _loc["ua"] = new Dictionary<string, string>
            {
                { "Launch", "ЗАПУСТИТИ" }, { "Kill", "ЗАКРИТИ" }, { "Home", "ГОЛОВНА" },
                { "FFlags", "FAST FLAGS" }, { "Settings", "НАЛАШТУВАННЯ" }, { "About", "ПРО ПРОГРАМУ" },
                { "HomeTitle", "Ласкаво просимо до NovaStrap!" }, { "RobloxOff", "Roblox: ⚫ Не запущено" },
                { "RobloxOn", "Roblox: 🟢 Запущено" }, { "FlagsCount", "Активних прапорців: " },
                { "Quick", "Швидкі дії:" }, { "QuickLaunch", "▶ Запустити Roblox" }, { "QuickReset", "🔄 Скинути прапорці" },
                { "FFlagsTitle", "⚡ FAST FLAGS EDITOR" }, { "Add", "+ Додати" }, { "Save", "💾 Зберегти" },
                { "Import", "📥 Імпорт" }, { "Export", "📤 Експорт" }, { "Clear", "🗑️ Очистити" }, { "Search", "Пошук..." },
                { "SettingsTitle", "⚙️ НАЛАШТУВАННЯ" }, { "LangTitle", "🌐 МОВА" }, { "RobloxPath", "ШЛЯХ ДО ROBLOX" },
                { "Browse", "ОГЛЯД" }, { "AutoDetect", "Автоматично знаходити Roblox" }, { "SaveSettings", "💾 ЗБЕРЕГТИ НАЛАШТУВАННЯ" },
                { "AboutTitle", "NOVASTRAP" }, { "AboutVer", "Версія 2.0" }, { "AboutDesc", "Потужний лаунчер для Roblox з підтримкою Fast Flags" },
                { "AboutCopy", "© 2025 NovaStrap Team" }, { "StatusReady", "✅ Готово" }, { "FlagHeader", "Прапорець" },
                { "ValueHeader", "Значення" }, { "TypeHeader", "Тип" }, { "FlagsSaved", "✅ Прапорці збережено" },
                { "RestartRoblox", "Прапорці збережено!\n\nПерезапустіть Roblox." }, { "ResetConfirm", "Скинути всі прапорці?" },
                { "ClearConfirm", "Видалити ВСІ прапорці?" }, { "FlagExists", "Такий прапорець вже існує!" },
                { "EmptyName", "Прапорець не може бути порожнім!" }, { "ImportSuccess", "Імпортовано {0} прапорців" },
                { "ExportSuccess", "Експортовано {0} прапорців" }, { "Success", "Успіх" }, { "Error", "Помилка" }
            };
            
            _loc["en"] = new Dictionary<string, string>
            {
                { "Launch", "LAUNCH" }, { "Kill", "KILL" }, { "Home", "HOME" }, { "FFlags", "FAST FLAGS" },
                { "Settings", "SETTINGS" }, { "About", "ABOUT" }, { "HomeTitle", "Welcome to NovaStrap!" },
                { "RobloxOff", "Roblox: ⚫ Not running" }, { "RobloxOn", "Roblox: 🟢 Running" }, { "FlagsCount", "Active flags: " },
                { "Quick", "Quick actions:" }, { "QuickLaunch", "▶ Launch Roblox" }, { "QuickReset", "🔄 Reset flags" },
                { "FFlagsTitle", "⚡ FAST FLAGS EDITOR" }, { "Add", "+ Add" }, { "Save", "💾 Save" }, { "Import", "📥 Import" },
                { "Export", "📤 Export" }, { "Clear", "🗑️ Clear" }, { "Search", "Search..." }, { "SettingsTitle", "⚙️ SETTINGS" },
                { "LangTitle", "🌐 LANGUAGE" }, { "RobloxPath", "ROBLOX PATH" }, { "Browse", "BROWSE" },
                { "AutoDetect", "Auto-detect Roblox" }, { "SaveSettings", "💾 SAVE SETTINGS" }, { "AboutTitle", "NOVASTRAP" },
                { "AboutVer", "Version 2.0" }, { "AboutDesc", "Powerful Roblox launcher with Fast Flags support" },
                { "AboutCopy", "© 2025 NovaStrap Team" }, { "StatusReady", "✅ Ready" }, { "FlagHeader", "Flag" },
                { "ValueHeader", "Value" }, { "TypeHeader", "Type" }, { "FlagsSaved", "✅ Flags saved" },
                { "RestartRoblox", "Flags saved!\n\nRestart Roblox." }, { "ResetConfirm", "Reset all flags?" },
                { "ClearConfirm", "Delete ALL flags?" }, { "FlagExists", "Flag already exists!" }, { "EmptyName", "Flag name cannot be empty!" },
                { "ImportSuccess", "Imported {0} flags" }, { "ExportSuccess", "Exported {0} flags" }, { "Success", "Success" }, { "Error", "Error" }
            };
        }

        private void LoadLanguage()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string langPath = Path.Combine(appData, "NovaStrap", "lang.txt");
            if (File.Exists(langPath)) _lang = File.ReadAllText(langPath).Trim();
            if (_lang != "ua" && _lang != "en") _lang = "ua";
            LangUA.IsChecked = _lang == "ua";
            LangEN.IsChecked = _lang == "en";
        }

        private void SaveLanguage()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string langPath = Path.Combine(appData, "NovaStrap", "lang.txt");
            File.WriteAllText(langPath, _lang);
        }

        private void LanguageChanged(object sender, RoutedEventArgs e)
        {
            _lang = LangUA.IsChecked == true ? "ua" : "en";
            SaveLanguage();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            var l = _loc[_lang];
            LaunchText.Text = l["Launch"]; KillText.Text = l["Kill"]; HomeText.Text = l["Home"];
            FFlagsText.Text = l["FFlags"]; SettingsText.Text = l["Settings"]; AboutText.Text = l["About"];
            HomeTitle.Text = l["HomeTitle"]; QuickTitle.Text = l["Quick"]; QuickLaunch.Content = l["QuickLaunch"];
            QuickReset.Content = l["QuickReset"]; FFlagsTitle.Text = l["FFlagsTitle"]; AddFlagBtn.Content = l["Add"];
            ApplyFlagsBtn.Content = l["Save"]; ImportBtn.Content = l["Import"]; ExportBtn.Content = l["Export"];
            ClearBtn.Content = l["Clear"]; SearchBox.Tag = l["Search"]; SettingsTitle.Text = l["SettingsTitle"];
            LangTitle.Text = l["LangTitle"]; RobloxPathText.Text = l["RobloxPath"]; BrowseBtn.Content = l["Browse"];
            AutoDetectBox.Content = l["AutoDetect"]; SaveSettingsBtn.Content = l["SaveSettings"]; AboutTitle.Text = l["AboutTitle"];
            AboutVer.Text = l["AboutVer"]; AboutDesc.Text = l["AboutDesc"]; AboutCopy.Text = l["AboutCopy"];
            StatusBar.Text = l["StatusReady"]; FlagCol.Header = l["FlagHeader"]; ValueCol.Header = l["ValueHeader"];
            TypeCol.Header = l["TypeHeader"]; UpdateStatus();
        }

        private void FindRobloxAutomatically()
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
                    if (latest != null && File.Exists(Path.Combine(latest, "RobloxPlayerBeta.exe")))
                    {
                        _robloxPath = latest;
                        RobloxPathBox.Text = _robloxPath;
                        StatusBar.Text = $"🎮 Roblox: {Path.GetFileName(_robloxPath)}";
                        return;
                    }
                }
            }
            StatusBar.Text = "⚠️ Roblox не знайдено!";
        }

        private string? GetSettingsPath()
        {
            if (!string.IsNullOrEmpty(_robloxPath) && Directory.Exists(_robloxPath))
                return Path.Combine(_robloxPath, "ClientSettings", "ClientAppSettings.json");
            FindRobloxAutomatically();
            return string.IsNullOrEmpty(_robloxPath) ? null : Path.Combine(_robloxPath, "ClientSettings", "ClientAppSettings.json");
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try { string json = File.ReadAllText(_settingsPath); var temp = JsonConvert.DeserializeObject<AppSettings>(json); if (temp != null) _settings = temp; } catch { }
            }
            RobloxPathBox.Text = _settings.RobloxPath ?? "";
            AutoDetectBox.IsChecked = _settings.AutoDetectRoblox;
            if (!string.IsNullOrEmpty(_settings.RobloxPath) && Directory.Exists(_settings.RobloxPath)) _robloxPath = _settings.RobloxPath;
        }

        private void SaveAppSettings()
        {
            try
            {
                _settings.RobloxPath = RobloxPathBox.Text;
                _settings.AutoDetectRoblox = AutoDetectBox.IsChecked ?? true;
                File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
                if (!string.IsNullOrEmpty(_settings.RobloxPath) && Directory.Exists(_settings.RobloxPath)) _robloxPath = _settings.RobloxPath;
                StatusBar.Text = "💾 Налаштування збережено";
            }
            catch { }
        }

        private string DetectType(string val)
        {
            if (bool.TryParse(val, out _)) return "Boolean";
            if (long.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) return "Number";
            return "String";
        }

        private void LoadFlags()
        {
            _flags.Clear();
            string? path = GetSettingsPath();
            if (path != null && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            string val = kv.Value?.ToString() ?? "";
                            _flags.Add(new FFlagEntry { Key = kv.Key, Value = val, Type = DetectType(val) });
                        }
                        StatusBar.Text = $"📁 Завантажено {_flags.Count} прапорців";
                    }
                }
                catch { }
            }
            if (_flags.Count == 0)
            {
                _flags.Add(new FFlagEntry { Key = "FFlagDebugGraphicsPreferVulkan", Value = "True", Type = "Boolean" });
                _flags.Add(new FFlagEntry { Key = "DFIntTaskSchedulerTargetFps", Value = "144", Type = "Number" });
            }
            ApplyFilter();
        }

        private void SaveFlags()
        {
            JObject settings = new JObject();
            foreach (var f in _flags)
            {
                object val = f.Value;
                if (f.Type == "Boolean") { bool.TryParse(f.Value, out bool b); val = b; }
                else if (f.Type == "Number") { long.TryParse(f.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out long l); val = l; }
                settings[f.Key] = JToken.FromObject(val);
            }
            
            string? path = GetSettingsPath();
            if (path == null)
            {
                MessageBox.Show("Roblox не знайдено!", _loc[_lang]["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            // Зберігаємо ВСІ флаги у правильному форматі
            string jsonOutput = settings.ToString(Formatting.Indented);
            File.WriteAllText(path, jsonOutput, new UTF8Encoding(false));
            
            StatusBar.Text = _loc[_lang]["FlagsSaved"];
            MessageBox.Show(_loc[_lang]["RestartRoblox"], _loc[_lang]["Success"], MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatus()
        {
            bool running = Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;
            RobloxStatus.Text = running ? _loc[_lang]["RobloxOn"] : _loc[_lang]["RobloxOff"];
            FlagsCount.Text = _loc[_lang]["FlagsCount"] + _flags.Count;
        }

        private void ApplyFilter()
        {
            _filteredFlags.Clear();
            string cat = CategoryCombo.SelectedItem?.ToString() ?? "📁 Усе";
            var filtered = _flags.Where(f =>
                (string.IsNullOrEmpty(_searchText) || f.Key.ToLower().Contains(_searchText.ToLower()) || f.Value.ToLower().Contains(_searchText.ToLower())) &&
                (cat == "📁 Усе" || cat == GetCategory(f.Key))
            );
            foreach (var f in filtered) _filteredFlags.Add(f);
            FilterStats.Text = $"{_filteredFlags.Count} / {_flags.Count}";
            FlagsStatus.Text = $"Показано {_filteredFlags.Count} з {_flags.Count}";
        }

        private string GetCategory(string key)
        {
            if (key.Contains("Graphics") || key.Contains("Render") || key.Contains("Shadow")) return "🎨 Graphics";
            if (key.Contains("FPS") || key.Contains("TaskScheduler") || key.Contains("Performance")) return "⚡ Performance";
            if (key.Contains("Network") || key.Contains("RakNet") || key.Contains("Ping")) return "🌐 Network";
            if (key.Contains("Debug") || key.Contains("Telemetry") || key.Contains("Dev")) return "🐛 Debug";
            if (key.Contains("UGC") || key.Contains("Validation")) return "👤 UGC";
            return "📁 Iнше";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            ApplyFilter();
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void HomeNavBtn_Click(object sender, RoutedEventArgs e)
        {
            HomePanel.Visibility = Visibility.Visible;
            FFlagsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;
        }

        private void FFlagsNavBtn_Click(object sender, RoutedEventArgs e)
        {
            HomePanel.Visibility = Visibility.Collapsed;
            FFlagsPanel.Visibility = Visibility.Visible;
            SettingsPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;
            ApplyFilter();
        }

        private void SettingsNavBtn_Click(object sender, RoutedEventArgs e)
        {
            HomePanel.Visibility = Visibility.Collapsed;
            FFlagsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
            AboutPanel.Visibility = Visibility.Collapsed;
        }

        private void AboutNavBtn_Click(object sender, RoutedEventArgs e)
        {
            HomePanel.Visibility = Visibility.Collapsed;
            FFlagsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Visible;
        }

        private void LaunchRobloxBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFlags();
            if (string.IsNullOrEmpty(_robloxPath)) FindRobloxAutomatically();
            if (!string.IsNullOrEmpty(_robloxPath))
            {
                string exe = Path.Combine(_robloxPath, "RobloxPlayerBeta.exe");
                if (File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
                    StatusBar.Text = "🚀 Roblox запущено";
                    UpdateStatus();
                    return;
                }
            }
            Process.Start(new ProcessStartInfo { FileName = "roblox://", UseShellExecute = true });
            StatusBar.Text = "🚀 Roblox запущено через протокол";
            UpdateStatus();
        }

        private void KillRobloxBtn_Click(object sender, RoutedEventArgs e)
        {
            int c = 0;
            foreach (var p in Process.GetProcessesByName("RobloxPlayerBeta")) { try { p.Kill(); c++; } catch { } }
            UpdateStatus();
            StatusBar.Text = c > 0 ? $"🛑 Закрито {c} процесів" : "❌ Roblox не запущено";
        }

        private void ResetFlagsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_loc[_lang]["ResetConfirm"], "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _flags.Clear();
                _flags.Add(new FFlagEntry { Key = "FFlagDebugGraphicsPreferVulkan", Value = "True", Type = "Boolean" });
                _flags.Add(new FFlagEntry { Key = "DFIntTaskSchedulerTargetFps", Value = "144", Type = "Number" });
                SaveFlags();
                ApplyFilter();
                UpdateStatus();
                StatusBar.Text = "🔄 Прапорці скинуто";
            }
        }

        private void AddFlagBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new InputDialog(_loc[_lang]["Add"], "Flag name:");
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
            {
                if (_flags.Any(f => f.Key == dlg.Answer))
                {
                    MessageBox.Show(_loc[_lang]["FlagExists"], _loc[_lang]["Error"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _flags.Add(new FFlagEntry { Key = dlg.Answer, Value = "True", Type = "Boolean" });
                ApplyFilter();
                StatusBar.Text = $"➕ Додано: {dlg.Answer}";
                UpdateStatus();
            }
        }

        private void RemoveFlag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FFlagEntry f)
            {
                _flags.Remove(f);
                ApplyFilter();
                StatusBar.Text = $"🗑️ Видалено: {f.Key}";
                UpdateStatus();
            }
        }

        private void ApplyFlagsBtn_Click(object sender, RoutedEventArgs e) => SaveFlags();
        
        private void ClearAllFlags_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_loc[_lang]["ClearConfirm"], "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _flags.Clear();
                ApplyFilter();
                UpdateStatus();
                StatusBar.Text = "🗑️ Всі прапорці видалено";
            }
        }

        private void ImportJsonFlags_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON/TXT|*.json;*.txt", Title = "Виберіть файл" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string content = File.ReadAllText(ofd.FileName);
                    Dictionary<string, object>? dict = null;
                    try { dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content); }
                    catch
                    {
                        dict = new Dictionary<string, object>();
                        foreach (string line in content.Split('\n', '\r'))
                        {
                            if (line.Contains('='))
                            {
                                var parts = line.Split('=');
                                if (parts.Length == 2) dict[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                    if (dict != null && dict.Count > 0)
                    {
                        int c = 0;
                        foreach (var kv in dict)
                        {
                            string val = kv.Value?.ToString() ?? "";
                            string t = DetectType(val);
                            var ex = _flags.FirstOrDefault(f => f.Key == kv.Key);
                            if (ex != null) { ex.Value = val; ex.Type = t; }
                            else { _flags.Add(new FFlagEntry { Key = kv.Key, Value = val, Type = t }); }
                            c++;
                        }
                        ApplyFilter();
                        UpdateStatus();
                        StatusBar.Text = string.Format(_loc[_lang]["ImportSuccess"], c);
                        MessageBox.Show(string.Format(_loc[_lang]["ImportSuccess"], c), _loc[_lang]["Success"], MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, _loc[_lang]["Error"], MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void ExportFlagsToJson_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = "flags" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var exp = new Dictionary<string, object>();
                    foreach (var f in _flags) exp[f.Key] = f.Value;
                    File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(exp, Formatting.Indented), new UTF8Encoding(false));
                    StatusBar.Text = string.Format(_loc[_lang]["ExportSuccess"], _flags.Count);
                    MessageBox.Show(string.Format(_loc[_lang]["ExportSuccess"], _flags.Count), _loc[_lang]["Success"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, _loc[_lang]["Error"], MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BrowseRobloxBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "RobloxPlayerBeta.exe|RobloxPlayerBeta.exe" };
            if (ofd.ShowDialog() == true)
            {
                string? path = Path.GetDirectoryName(ofd.FileName);
                RobloxPathBox.Text = path;
                _robloxPath = path ?? "";
                StatusBar.Text = $"📁 {path}";
                LoadFlags();
            }
        }

        private void SaveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveAppSettings();
            LoadFlags();
            MessageBox.Show(_loc[_lang]["Success"], "", MessageBoxButton.OK, MessageBoxImage.Information);
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
            box.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) { Answer = box.Text; DialogResult = true; } };
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
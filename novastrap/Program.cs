using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Novastrap
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                try
                {
                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        MessageBox.Show("Не удалось определить путь к программе!", 
                            "NovaStrap", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                        return;
                    }
                    
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch
                {
                    MessageBox.Show("Для работы программы требуются права администратора!", 
                        "NovaStrap", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }
            }
            
            var app = new App();
            var window = new MainWindow();
            app.Run(window);
        }
    }
}
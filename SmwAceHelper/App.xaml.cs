using SmwAceHelper.Properties;
using SmwAceHelper.Views;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Windows;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;

namespace SmwAceHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            MainView mainView = new MainView();
            mainView.Show();
            mainView.UpdateWindowSize();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StringBuilder log = new StringBuilder();
            using (ManagementClass managementClass = new ManagementClass("Win32_OperatingSystem"))
            {
                using (ManagementObjectCollection collection = managementClass.GetInstances())
                {
                    using (ManagementObject? managementObject = collection.OfType<ManagementObject>().FirstOrDefault())
                    {
                        if (managementObject != null)
                        {
                            log.Append(managementObject["Caption"] as string);
                            log.Append(" ");
                            log.AppendLine(managementObject["Version"] as string);
                        }
                    }
                }
            }
            log.Append("SMW ACE Helper");
            log.Append(" ");
            log.AppendLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString());
            log.Append("CurrentRegion: ");
            log.AppendLine(RegionInfo.CurrentRegion.ToString());
            log.Append("CurrentCulture: ");
            log.AppendLine(CultureInfo.CurrentCulture.ToString());
            log.Append("CurrentUICulture: ");
            log.AppendLine(CultureInfo.CurrentUICulture.ToString());
            log.Append("Language: ");
            log.AppendLine(LocalizeDictionary.Instance.Culture.ToString());
            log.AppendLine();
            log.AppendLine(e.ExceptionObject.ToString());

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmwAceHelper\\CrashReport");
            string path = Path.Combine(dir, "CrashReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".txt");
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, log.ToString(), Encoding.UTF8);
            Process.Start("explorer.exe", dir);

            MessageBox.Show(
                LocExtension.GetLocalizedValue<string>(nameof(StringResources.CRASHED)),
                LocExtension.GetLocalizedValue<string>(nameof(StringResources.TITLE)),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

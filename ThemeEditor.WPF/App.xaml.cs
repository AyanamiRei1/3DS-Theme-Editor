﻿// --------------------------------------------------
// 3DS Theme Editor - App.xaml.cs
// --------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

using ThemeEditor.WPF.Properties;

namespace ThemeEditor.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
#if !DEBUG
            args.Handled = true;
            var wnd = new CrashWindow(args.Exception);
            wnd.ShowDialog();
            Environment.Exit(1);
#endif
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
        public string[] LaunchArgs { get; private set; }
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            LaunchArgs = e.Args;
#if DEBUG
            if (e.Args.Length != 0)
            {
                try
                {
                    string cultureStr = e.Args[0];
                    var culture = new CultureInfo(cultureStr);

                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                    FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                        new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
                }
                catch (CultureNotFoundException) {}
            }

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            if (e.Args.Length > 1 && File.Exists(e.Args[1]))
                mainWindow.LoadThemeCommandWrapper.Command.Execute(e.Args[1]);
            else
                mainWindow.NewThemeCommandWrapper.Command.Execute(null);

#else

            var userDir = "./UserData";
            Directory.CreateDirectory(userDir);

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            if (e.Args.Length > 0 && File.Exists(e.Args[0]))
                mainWindow.LoadThemeCommandWrapper.Command.Execute(e.Args[0]);
            else
                mainWindow.NewThemeCommandWrapper.Command.Execute(null);
#endif
        }
    }
}

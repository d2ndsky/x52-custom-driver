using System;
using System.Windows;
using X52.CustomDriver.Core.Interfaces;
using X52.CustomDriver.Core.Services;
using X52.CustomDriver.App.ViewModels;
using System.Windows.Forms; // Needed for NotifyIcon

namespace X52.CustomDriver.App
{
    public partial class App : System.Windows.Application
    {
        private IHidService? _hidService;
        private IVJoyService? _vJoyService;
        private ProfileService? _profileService;
        private NotifyIcon? _notifyIcon;

        public static bool IsExiting { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_log.txt");
            System.IO.File.WriteAllText(logPath, "--- Startup Log ---\n");

            try
            {
                System.IO.File.AppendAllText(logPath, "Initializing Services...\n");
                _vJoyService = new VJoyService();
                _hidService = new X52HidService();
                _profileService = new ProfileService();
                var settingsService = new SettingsService();

                System.IO.File.AppendAllText(logPath, "Connecting Hardware...\n");
                
                // Initialize vJoy and check for success
                if (!_vJoyService.Initialize(1))
                {
                    System.Windows.MessageBox.Show("Could not initialize vJoy Device #1.\n\nPossible reasons:\n- vJoy is not installed.\n- vJoy Device #1 is not enabled in Configure vJoy.\n- Another application is using it exclusively.\n\nPlease install/configure vJoy from http://vjoystick.sourceforge.net", "vJoy Init Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                _hidService.Initialize();
                _profileService.StartWatcher();
                
                System.IO.File.AppendAllText(logPath, "Hardware Connected. Starting Listener...\n");
                if (_hidService.IsConnected)
                {
                    _hidService.StartListening();
                }

                System.IO.File.AppendAllText(logPath, "Setup Tray Icon...\n");
                _notifyIcon = new NotifyIcon();
                try 
                {
                    string iconPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(iconPath))
                        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(iconPath);
                }
                catch (Exception exIcon) { System.IO.File.AppendAllText(logPath, $"Icon Error: {exIcon.Message}\n"); }
                
                _notifyIcon.Visible = true;
                _notifyIcon.Text = "Ærakon x52 driver";
                _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Open", null, (s, args) => ShowMainWindow());
                contextMenu.Items.Add("Exit", null, (s, args) => { IsExiting = true; System.Windows.Application.Current.Shutdown(); });
                _notifyIcon.ContextMenuStrip = contextMenu;

                System.IO.File.AppendAllText(logPath, "Starting UI...\n");
                var viewModel = new X52ViewModel(_hidService, _vJoyService, _profileService!, settingsService);
                var mainWindow = new MainWindow(viewModel);
                System.IO.File.AppendAllText(logPath, "Showing MainWindow...\n");

                MainWindow = mainWindow;
                mainWindow.Show();

                System.IO.File.AppendAllText(logPath, "Startup Completed Successfully.\n");
            }
            catch (Exception ex)
            {
                string error = $"FATAL ERROR:\n{ex.Message}\n{ex.StackTrace}\n";
                System.IO.File.AppendAllText(logPath, error);
                System.Windows.MessageBox.Show(error, "X52 Driver Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hidService?.StopListening();
            _vJoyService?.Shutdown();
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}

using System;
using System.Windows;
using X52.CustomDriver.App.ViewModels;

namespace X52.CustomDriver.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(X52ViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (DataContext is X52ViewModel vm && vm.MinimizeToTray)
                {
                    this.Hide();
                }
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!App.IsExiting)
            {
                if (DataContext is X52ViewModel vm && vm.CloseToTray)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            }
            base.OnClosing(e);
        }

        private void CalibrateCenter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is X52ViewModel vm) vm.CalibrateCenter();
        }

        private void ResetCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is X52ViewModel vm) vm.ResetCalibration();
        }

        private void EditMappings_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is X52ViewModel vm)
            {
                var editor = new MappingEditorWindow(vm);
                editor.Owner = this;
                editor.ShowDialog();
            }
        }
    }
}
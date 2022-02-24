
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EnRagedGUI.Pages.SettingsContent
{
    /// <summary>
    /// Interaction logic for General.xaml
    /// </summary>
    public partial class General : Page
    {
        public General()
        {
            InitializeComponent();
            ToggleSwitch.IsChecked = Properties.Settings.Default.KillSwitch;

        }

        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ToggleSwitch.IsChecked)
            {
                // Code for Checked state
                Properties.Settings.Default.KillSwitch = true;
                Properties.Settings.Default.Save();

            }
            else
            {
                // Code for Un-Checked state
                Properties.Settings.Default.KillSwitch = false;
                Properties.Settings.Default.Save();
            }
        }

        private void LogsSwitch_Click(object sender, RoutedEventArgs e)
        {

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.Content = new Dashboard(true);

        }
    }
}

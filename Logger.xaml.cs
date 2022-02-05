using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static EnRagedGUI.Globals;
namespace EnRagedGUI
{
    /// <summary>
    /// Interaction logic for Logger.xaml
    /// </summary>
    public partial class Logger : Window
    {

        public Logger()
        {
            InitializeComponent();
        }


        public void test(string a)
        {
            Dispatcher.Invoke(new Action<string>(logBox.AppendText), new object[] { a + "\r\n" });
        }
    }

       
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace AntiSanction
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void changeDnsBtn_Click(object sender, RoutedEventArgs e)
        {
            string str = ServerList.Text;
            ComboBoxItem cbi = (ComboBoxItem)ServerList.SelectedItem;
            string str1 = cbi.Content.ToString();
            //string val = ServerList.SelectedValue.ToString();
            //MessageBox.Show(str1);
            var network = new NetworkConfigurator();
            var nic = network.GetNIC();
            if (str1 == "شکن")
            {
                network.SetDns(nic.Description, "178.22.122.100", "185.51.200.2");
            }
            else if (str1 == "403 آنلاین")
            {
                network.SetDns(nic.Description, "10.202.10.202", "10.202.10.102");
            }
            else if (str1 == "بگذر")
            {
                network.SetDns(nic.Description, "185.55.225.25", "185.55.225.25");
            }
            else if (str1 == "رادار گیم")
            {
                network.SetDns(nic.Description, "10.202.10.10", "10.202.10.11");
            }
            else if (str1 == "الکتروم")
            {
                network.SetDns(nic.Description, "78.157.42.100", "78.157.42.101");
            }
            else if (str1 == "پیشگامان")
            {
                network.SetDns(nic.Description, "5.202.100.100", "5.202.100.101");
            }
            else if (str1 == "شاتل")
            {
                network.SetDns(nic.Description, "85.15.1.14", "85.15.1.15");
            }
            else if (str1 == "هاست ایران")
            {
                network.SetDns(nic.Description, "172.29.0.100", "172.29.2.100");
            }
            MessageBox.Show("دی‌ان‌اس ست شد");
        }

        private void UnsetBtn_Click(object sender, RoutedEventArgs e)
        {
            var network = new NetworkConfigurator();
            var nic = network.GetNIC();
            network.UnsetDNS(nic.Description);
            MessageBox.Show("دی‌ان‌اس برداشته شد");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { DragMove(); };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }

}

using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace DashBoard.Controls
{
    /// <summary>
    /// Interaction logic for Traders.xaml
    /// </summary>
    public partial class Traders : UserControl
    {
        public List<Main.Trader> BanList = new List<Main.Trader>();
        public Main MainWindow { get; set; }

        public Traders()
        {
            InitializeComponent();

        }

        public void Refresh()
        {
            tradersDataGrid.Items.Refresh();
        }

        private void onContextShown(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool bannable = false;
            bool unbannable = false;

            foreach (Main.Trader trader in tradersDataGrid.SelectedItems)
            {
                if (trader.Banned)
                {
                    unbannable = true;
                }
                else
                {
                    bannable = true;
                }
            }

            if (bannable)
            {
                BanTradersMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                BanTradersMenuItem.Visibility = Visibility.Collapsed;
            }

            if (unbannable)
            {
                UnbanTradersMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                UnbanTradersMenuItem.Visibility = Visibility.Collapsed;
            }

        }


        private void BanTraders(object sender, RoutedEventArgs e)
        {
            BanList.Clear();

            foreach (Main.Trader trader in tradersDataGrid.SelectedItems)
            {
                if (BanList.Where(t => t.LastIP == trader.LastIP && t.Logon == trader.Logon).Count() == 0)
                {
                    BanList.Add(MainWindow.TraderList.Where(t => t.Game == trader.Game & t.Logon == trader.Logon).First());
                }
            }

            Pages.BanTraders banTraders = new Pages.BanTraders(BanList);
            banTraders.Left = MainWindow.Left + 100;
            banTraders.Top = MainWindow.Top + 200;
            banTraders.MainWindow = MainWindow;
            banTraders.ShowDialog();
        }

        private void UnbanTraders(object sender, RoutedEventArgs e)
        {
            BanList.Clear();

            foreach (Main.Trader trader in tradersDataGrid.SelectedItems)
            {
                if (BanList.Where(t => t.LastIP == trader.LastIP && t.Logon == trader.Logon).Count() == 0)
                {
                    BanList.Add(MainWindow.TraderList.Where(t => t.Game == trader.Game & t.Logon == trader.Logon).First());
                }
            }

            Pages.UnbanTraders unbanTraders = new Pages.UnbanTraders(BanList);
            unbanTraders.Left = MainWindow.Left + 100;
            unbanTraders.Top = MainWindow.Top + 200;
            unbanTraders.MainWindow = MainWindow;
            unbanTraders.ShowDialog();
        }
    }

    [ValueConversion(typeof(string), typeof(String))]
    public class AddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format("{0}:{1}", (string)value, (string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return DependencyProperty.UnsetValue;
        }
    }
}

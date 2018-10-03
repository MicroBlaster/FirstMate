using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;

namespace DashBoard.Controls
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// </summary>
    public partial class Overview : UserControl
    {
        public List<Main.Trader> BanList = new List<Main.Trader>();
        public Main MainWindow { get; set; }

        public Overview()
        {
            InitializeComponent();

            int columnIndex = 0;

            // Sort the datagrid by the Log Date
            var column = ActivityDataGrid.Columns[columnIndex];
            ActivityDataGrid.Items.SortDescriptions.Clear();
            ActivityDataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));
            foreach (var col in ActivityDataGrid.Columns) col.SortDirection = null;
            column.SortDirection = ListSortDirection.Descending;

        }

        public void Refresh()
        {
            ActivityDataGrid.Items.Refresh();
        }

        private void BanTraders(object sender, RoutedEventArgs e)
        {
            BanList.Clear();

            foreach (Main.Activity activity in ActivityDataGrid.SelectedItems)
            {
                string[] IP = activity.Address.Split('/')[0].Split('.');

                if (IP.Count() > 2)
                {
                    string Address = $"{IP[0]}.{IP[1]}.{IP[2]}.*";

                    foreach (Main.Address address in MainWindow.AddressList.Where(a => a.IP == Address))
                    {
                        foreach (Main.Trader trader in MainWindow.TraderList.Where(t => t.Game == address.Game & t.Logon == address.Logon))
                        {
                            if (BanList.Where(t => t.LastIP == trader.LastIP && t.Logon == trader.Logon).Count() == 0)
                            {
                                BanList.Add(trader);
                            }
                        }
                    }
                }
            }

            Pages.BanTraders banTraders = new Pages.BanTraders(BanList);
            banTraders.MainWindow = MainWindow;
            banTraders.Left = MainWindow.Left + 100;
            banTraders.Top = MainWindow.Top + 200;
            banTraders.ShowDialog();
        }

        private void UnbanTraders(object sender, RoutedEventArgs e)
        {
            BanList.Clear();

            foreach (Main.Activity activity in ActivityDataGrid.SelectedItems)
            {
                string[] IP = activity.Address.Split('/')[0].Split('.');

                if (IP.Count() > 2)
                {
                    string Address = $"{IP[0]}.{IP[1]}.{IP[2]}.*";

                    foreach (Main.Address address in MainWindow.AddressList.Where(a => a.IP == Address))
                    {
                        foreach (Main.Trader trader in MainWindow.TraderList.Where(t => t.Game == address.Game & t.Logon == address.Logon))
                        {
                            if (BanList.Where(t => t.LastIP == trader.LastIP && t.Logon == trader.Logon).Count() == 0)
                            {
                                BanList.Add(trader);
                            }
                        }
                    }
                }
            }

            Pages.UnbanTraders unbanTraders = new Pages.UnbanTraders(BanList);
            unbanTraders.MainWindow = MainWindow;
            unbanTraders.Left = MainWindow.Left + 100;
            unbanTraders.Top = MainWindow.Top + 200;
            unbanTraders.ShowDialog();
        }

        private void onContextShown(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool bannable = false;

            foreach (Main.Activity activity in ActivityDataGrid.SelectedItems)
            {
                if(activity.Bannable)
                {
                    bannable = true;
                    break;
                }
            }

            BanTradersMenuItem.IsEnabled = bannable;
            UnbanTradersMenuItem.IsEnabled = bannable;
        }
    }
}

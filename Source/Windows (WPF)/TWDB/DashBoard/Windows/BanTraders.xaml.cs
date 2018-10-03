using Microsoft.Win32;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace DashBoard.Pages
{
    /// <summary>
    /// Interaction logic for Fraud.xaml
    /// </summary>
    public partial class BanTraders : Window, INotifyPropertyChanged
    {
        public List<Main.Trader> BanList = new List<Main.Trader>();
        public Main MainWindow { get; set; }

        private bool allItemsAreChecked;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AllItemsAreChecked
        {
            get
            {
                return this.allItemsAreChecked;
            }
            set
            {
                this.allItemsAreChecked = value;
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("AllItemsAreChecked"));
                }
            }
        }

        public BanTraders(List<Main.Trader> banList)
        {
            InitializeComponent();
            // Get the file modified date, and generate appVersion from this date.
            DateTime CompiledDate = File.GetLastWriteTimeUtc(Environment.CurrentDirectory + "\\dashboard.exe");

            BanList = banList;
            tradersDataGrid.ItemsSource = BanList;
        }

        private void onOkClick(object sender, RoutedEventArgs e)
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            INetFwRule rule = null;
            try
            {
                rule = firewallPolicy.Rules.Item("TW2002 Banned Traders");
            }
            catch { }

            if (rule == null)
            {
                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                firewallRule.Name = "TW2002 Banned Traders";
                firewallRule.Description = "Holds a list of tarders banned from TradeWars 2002.";
                firewallRule.InterfaceTypes = "All";
                firewallRule.LocalPorts = "2002";
                firewallPolicy.Rules.Add(firewallRule);
                rule = firewallPolicy.Rules.Item("TW2002 Banned Traders");
            }

            rule.Enabled = true;

            foreach (Main.Trader trader in BanList.Where(t => t.Banned == true))
            {
                List<Main.Trader> traders = MainWindow.TraderList.Where(t => t.Logon == trader.Logon).ToList();
                foreach (Main.Trader t in traders)
                {
                    t.Banned = true;
                }

                foreach (Main.Address address in MainWindow.AddressList.Where(a => a.Game == trader.Game & a.Logon == trader.Logon))
                {
                    string s = address.IP.Replace("*", "0/255.255.255.0");

                    if (!rule.RemoteAddresses.Contains(s))
                    {
                        try
                        {
                            if (rule.RemoteAddresses == "*")
                            {
                                rule.RemoteAddresses = s;
                            }
                            else
                            {
                                rule.RemoteAddresses += $",{s}";
                            }
                        }
                        catch (Exception ex) { }
                    }
                }
            }
            MainWindow.TradersControl.tradersDataGrid.Items.Refresh();
            Close();
        }


        private void onCancelClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.FraudDetection = false;
            this.Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SelectAllClicked(object sender, RoutedEventArgs e)
        {
            foreach (Main.Trader trader in BanList)
            {
                if (((CheckBox)sender).IsChecked == true)
                {
                    trader.Banned = true;
                }
                else
                {
                    trader.Banned = false;
                }
            }
            tradersDataGrid.Items.Refresh();
        }

        private void BannedCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            //CheckBox selectall = (CheckBox)tradersDataGrid   .HeaderRow.FindControl("SelectAllCheckBox");
            //CheckBox selectall = (CheckBox)tradersDataGrid.Resources.FindName("SelectAllCheckBox");
            //CheckBox selectall = (CheckBox)tradersDataGrid.Columns[0].HeaderTemplate.FindName("SelectAllCheckBox", tradersDataGrid);

   
            if (BanList.Where(t => t.Banned == false).Count() > 0)
            {
                //selectall.IsChecked = false;
                // TODO: find out why I can't see this.
                //SelectAllCheckBox.
                // MVVM way doesn't work either
                AllItemsAreChecked = false;
            }
            else
            {
                //selectall.IsChecked = true;
                AllItemsAreChecked = true;
            }
            tradersDataGrid.Items.Refresh();
        }
    }
}

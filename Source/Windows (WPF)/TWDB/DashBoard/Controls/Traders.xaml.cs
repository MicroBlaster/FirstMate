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

namespace DashBoard.Controls
{
    /// <summary>
    /// Interaction logic for Traders.xaml
    /// </summary>
    public partial class Traders : UserControl
    {
        private Main mainwindow;
        public Main MainWindow
        {
            set
            {
                mainwindow = value;
                //tradersDataGrid.ItemsSource = mainwindow.TraderList;
            }
        }

        public Traders()
        {
            InitializeComponent();

        }

        public void Refresh()
        {
            tradersDataGrid.Items.Refresh();
        }
    }
}

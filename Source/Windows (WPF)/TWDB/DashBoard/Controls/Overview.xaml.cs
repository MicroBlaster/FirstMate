using System.Windows.Controls;

namespace DashBoard.Controls
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// </summary>
    public partial class Overview : UserControl
    {
        private Main mainwindow;
        public Main MainWindow
        {
            set
            {
                mainwindow = value;
                //gamesDataGrid.ItemsSource = mainwindow.GameList;
                //EnableCollectionSynchronization();
            }
        }

        public Overview()
        {
            InitializeComponent();

        }

        public void Refresh()
        {
            gamesDataGrid.Items.Refresh();
        }

    }
}

using System.Windows.Controls;

namespace DashBoard.Controls
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// </summary>
    public partial class Games : UserControl
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

        public Games()
        {
            InitializeComponent();

        }

        public void Refresh()
        {
            gamesDataGrid.Items.Refresh();
        }

    }
}

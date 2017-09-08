using System.Windows;
using Terminal;

namespace TWFM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();

            Display console = new Display(160,48);

            
        }
    }
}

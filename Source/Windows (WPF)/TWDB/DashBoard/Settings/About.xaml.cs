using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace DashBoard
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            // Get the file modified date, and generate appVersion from this date.
            DateTime CompiledDate = File.GetLastWriteTimeUtc(Environment.CurrentDirectory + "\\dashboard.exe");

            aboutTextBlock.Text = $"TradeWars DashBoard - Version {CompiledDate:yy}{CompiledDate.DayOfYear / 7:00} " +
                                  $"(Build {(CompiledDate.DayOfYear * 24) + CompiledDate.Hour:0000})\n" +
                                  "© 2018 David McCartney. All Rights Reserved\n\n" + 
                                  "TradeWars is a registered trademark of\nEpic Interactive Strategy.\n\n" +
                                  "Free fraud detection provided by:\n\n" +
                                  "     https://ipqualityscore.com/ \n" +
                                  "     https://getipintel.net";
        }

        private void onOkClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}

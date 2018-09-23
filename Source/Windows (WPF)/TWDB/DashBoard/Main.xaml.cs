using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.ComponentModel;

namespace DashBoard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {

        #region Globals

        About aboutWindow = new About();

        // TWGS Information
        string Version, ServerRoot;

        // Data Lists
        public List<Game> GameList = new List<Game>();
        public List<Trader> TraderList = new List<Trader>();
        public List<Provider> ProviderList = new List<Provider>();
        public List<Address> AddressList = new List<Address>();

        // Background Worker
        BackgroundWorker logWorker;
        BackgroundWorker fraudWorker;

        // Store the mouse state for title bar drag, snap, and maximize events.
        bool UnSnap = false;
        Point MousePosition;

        #endregion
        #region System

        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        const int WM_SYSCOMMAND = 0x112;
        HwndSource hwndSource;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #endregion
        #region Initialization

        public Main()
        {
            InitializeComponent();

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            innerBorder.Visibility = Visibility.Visible;
            outerBorder.Visibility = Visibility.Visible;

            // Event hander to initialize window source.
            SourceInitialized += new EventHandler(InitializeWindowSource);

            LoadSettings();

            OverviewControl.MainWindow = this;
            //OverviewControl.gamesDataGrid.IsEnabled = false;

            TradersControl.MainWindow = this;
            //TradersControl.tradersDataGrid.IsEnabled = false;

            fraudWorker = new BackgroundWorker();
            fraudWorker.WorkerSupportsCancellation = true;
            fraudWorker.WorkerReportsProgress = true;
            fraudWorker.ProgressChanged += fraudWorkerProgress;
            fraudWorker.RunWorkerCompleted += fraudWorkerCompleted;
            fraudWorker.DoWork += fraudWorkerDoWork;

            logWorker = new BackgroundWorker();
            logWorker.WorkerSupportsCancellation = true;
            logWorker.WorkerReportsProgress = true;
            logWorker.ProgressChanged += logWorkerProgress;
            logWorker.RunWorkerCompleted += logWorkerCompleted;
            logWorker.DoWork += logWorkerDoWork;

            logWorker.RunWorkerAsync();
        }

        private void onWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Maximized)
            {
                SwitchState();
            }

            
        }

        private void LoadSettings()
        {
            // Get the file modified date, and generate appVersion from this date.
            //DateTime CompiledDate = File.GetLastWriteTimeUtc(Environment.CurrentDirectory + "\\dw.exe");
            //String appVersion = String.Format("{0:yy}.{1}.{2:0000}", CompiledDate, CompiledDate.DayOfYear / 7, (CompiledDate.DayOfYear * 24) + CompiledDate.Hour);
            //versionLabel.Content = "Version " + appVersion;
            versionLabel.Visibility = Visibility.Hidden;

            if (Properties.Settings.Default.FirstRun)
            {
                Properties.Settings.Default.FirstRun = false;
            }
            else
            {
                // Apply window position.
                Left = Properties.Settings.Default.Left;
                Top = Properties.Settings.Default.Top;
                Width = Properties.Settings.Default.Width;
                Height = Properties.Settings.Default.Height;
            }

            if(Properties.Settings.Default.FraudDetection)
            {
                FraudDetectionMenuItem.IsChecked = true;
            }

            string regkey;
            if (Environment.Is64BitOperatingSystem == true)
            {
                regkey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Epic Interactive Strategy\Trade Wars 2002 Game Server\Configuration";
            }
            else
            {
                regkey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Epic Interactive Strategy\Trade Wars 2002 Game Server\Configuration";
            }

            Version = (string)Registry.GetValue(regkey, "Version", "");
            ServerRoot = (string)Registry.GetValue(regkey, "ServerRoot", "");

            if (Version == "")
            {
                System.Windows.MessageBox.Show("TradeWars Game Server does not appear to be installed.\nPlease run from the same machine as TWGS.", "Error");
                return;
            }

            if (Environment.Is64BitOperatingSystem == true)
            {
                regkey = @"SOFTWARE\WOW6432Node\Epic Interactive Strategy\Trade Wars 2002 Game Server\Games";
            }
            else
            {
                regkey = @"SOFTWARE\Epic Interactive Strategy\Trade Wars 2002 Game Server\Games";
            }

            RegistryKey gamesRegKey = Registry.LocalMachine.OpenSubKey(regkey);
            if (gamesRegKey != null)
            {
                String[] subkeyNames = gamesRegKey.GetSubKeyNames();

                foreach (String name in subkeyNames)
                {
                    string subkey = "HKEY_LOCAL_MACHINE\\" + regkey + "\\" + name;
                    if ((string)Registry.GetValue(subkey, "Active", "") == "T")
                    {
                        GameList.Add(new Game() {
                            Name = name,
                            Title = (string)Registry.GetValue(subkey, "Title", ""),
                            Description = (string)Registry.GetValue(subkey, "Description", ""),
                            Directory = (string)Registry.GetValue(subkey, "Directory", "")});
                    }
                }
            }

            if (GameList.Count == 0)
            {
                System.Windows.MessageBox.Show("No active games found. Please activate one or more games in the Command Center.", "Error");
                return;
            }

        }


        #endregion
        #region Event Handlers

        private void InitializeWindowSource(object sender, EventArgs e)
        {
            // Get a handle to the window source so we can re-size it.
            hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            // Save window position.
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Left = Left;
                Properties.Settings.Default.Top = Top;
                Properties.Settings.Default.Width = Width;
                Properties.Settings.Default.Height = Height;
            }


            Properties.Settings.Default.Save();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //TODO: Restore down if already max height

                this.Top = 0;
                this.Height = 1024;

                //TODO: Get/Apply actual screen height
            }
            windowResizing(true);
        }


        private void onMouseMove(object sender, MouseEventArgs e)
        {
            windowResizing(false);
        }

        private void onMouseUp(object sender, MouseButtonEventArgs e)
        {

            // resize the background to fit the new windows size
            //MainTheme.backgroundImage.Height = this.Height - 64;
            //MainTheme.backgroundRect.Height = this.Height - 64;
        }

        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void windowResizing(bool resizing)
        {
            const int BORDER_WIDTH = 8;

            //if (window == null) return;

            ResizeDirection d = 0;
            Point mousePosition = Mouse.GetPosition(this);
            //_textBox1.Text = mousePosition.X + ":" + mousePosition.Y + "\n" + _window.Width + "\n" + this.Width;

            if (mousePosition.X < BORDER_WIDTH)  // Left
                d = ResizeDirection.Left;

            if (mousePosition.X > (this.Width - BORDER_WIDTH)) // Right
                d = ResizeDirection.Right;


            if (mousePosition.Y < BORDER_WIDTH) //Top
            {
                d = ResizeDirection.Top;

                if (mousePosition.X < BORDER_WIDTH) // Top Left
                    d = ResizeDirection.TopLeft;

                if (mousePosition.X > (this.Width - BORDER_WIDTH)) // Top Right
                    d = ResizeDirection.TopRight;
            }

            if (mousePosition.Y > (this.Height - BORDER_WIDTH)) // Bottom
            {
                d = ResizeDirection.Bottom;

                if (mousePosition.X < BORDER_WIDTH) // Bottom Left
                    d = ResizeDirection.BottomLeft;

                if (mousePosition.X > (this.Width - BORDER_WIDTH)) // Bottom Right
                    d = ResizeDirection.BottomRight;
            }

            // Send a windows system message to resize the window if the left mouse button is down.
            if (resizing)
            {
                SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + d), IntPtr.Zero);
            }
            else
            {
                switch (d)
                {
                    case ResizeDirection.Left:
                    case ResizeDirection.Right:
                        this.Cursor = Cursors.SizeWE;
                        break;

                    case ResizeDirection.Top:
                    case ResizeDirection.Bottom:
                        this.Cursor = Cursors.SizeNS;
                        break;

                    case ResizeDirection.TopLeft:
                    case ResizeDirection.BottomRight:
                        this.Cursor = Cursors.SizeNWSE;
                        break;

                    case ResizeDirection.TopRight:
                    case ResizeDirection.BottomLeft:
                        this.Cursor = Cursors.SizeNESW;
                        break;
                }
            }
        }

        private void onTitleClick(object sender, MouseButtonEventArgs e)
        {
            // Change window state if double clicked
            if (e.ClickCount == 2)
            {
                if (e.GetPosition(this).Y < 60)
                {
                    SwitchState();
                }
            }
            else
            {
                if (this.WindowState == System.Windows.WindowState.Maximized)
                {
                    UnSnap = true;
                    MousePosition = e.GetPosition(this);
                }
            }
        }

        private void onTitleRelease(object sender, MouseButtonEventArgs e)
        {
            UnSnap = false;
        }

        private void onTitleDrag(object sender, MouseEventArgs e)
        {
            if (UnSnap)
            {
                Point p = e.GetPosition(this);
                if (p.X - MousePosition.X > 5 ||
                    p.X - MousePosition.X < -5 ||
                    p.Y - MousePosition.Y > 5 ||
                    p.Y - MousePosition.Y < -5)
                {

                    double X = p.X - RestoreBounds.Width / 2;
                    if (X < 0) X = 0;
                    if (X + RestoreBounds.Width > SystemParameters.PrimaryScreenWidth) X = SystemParameters.PrimaryScreenWidth - RestoreBounds.Width;

                    //re-position on second monitor
                    if (Left > SystemParameters.PrimaryScreenWidth) X += SystemParameters.PrimaryScreenWidth;

                    UnSnap = false;
                    SwitchState();
                    Top = 0;
                    Left = X;
                }
            }
            else
            {
                // Allow dragging by the title bar.
                if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y < 60)
                {
                    DragMove();
                }
            }
        }

        private void onAppClose(object sender, RoutedEventArgs e)
        {
            //Close();
            Application.Current.Shutdown();
        }

        private void onAppMinimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void onAppMaximize(object sender, RoutedEventArgs e)
        {
            SwitchState();
        }

        private void onAppRestore(object sender, RoutedEventArgs e)
        {
            SwitchState();
        }

        private void SwitchState()
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                this.WindowState = System.Windows.WindowState.Maximized;
                cmdMaximizeApp.Visibility = Visibility.Hidden;
                cmdRestoreApp.Visibility = Visibility.Visible;

                innerBorder.Visibility = Visibility.Hidden;
                outerBorder.Visibility = Visibility.Hidden;

                //This doesn't work, because you can't modify the height of a window this way.
                //this.Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

                // TODO: Get working area of active monitor, and re-adjust when working area changes.
                System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

                Thickness maximizedMargin = new Thickness
                {
                    Top = workingArea.Top + 6,
                    Left = workingArea.Left + 6,
                    Bottom = this.ActualHeight - workingArea.Height - workingArea.Top - 7,
                    Right = this.ActualWidth - workingArea.Width - workingArea.Left - 7
                };

                mainGrid.Margin = maximizedMargin;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
                cmdMaximizeApp.Visibility = Visibility.Visible;
                cmdRestoreApp.Visibility = Visibility.Hidden;

                innerBorder.Visibility = Visibility.Visible;
                outerBorder.Visibility = Visibility.Visible;

                mainGrid.Margin = new Thickness(4);
            }
        }

        private void onCustomizeClick(object sender, RoutedEventArgs e)
        {
            // Show Customization Menu
            if (customizeMenu.Visibility == Visibility.Hidden)
            {
                customizeMenu.Visibility = Visibility.Visible;
                customizeMenu.IsOpen = true;

                customizeMenu.PlacementTarget = customize;
                customizeMenu.Placement = PlacementMode.RelativePoint;
                customizeMenu.HorizontalOffset = -230;
                customizeMenu.VerticalOffset = 25;
            }
            else
            {
                customizeMenu.Visibility = Visibility.Hidden;
            }
        }

        private void onCustomizeClosed(object sender, RoutedEventArgs e)
        {
            customizeMenu.Visibility = Visibility.Hidden;

        }

        private void onCustomizeLostFocus(object sender, RoutedEventArgs e)
        {
            // TODO: Close customization menu when focus is lost
            //customizeMenu.Visibility = Visibility.Hidden;
        }


        private void OverviewMouseEnter(object sender, MouseEventArgs e)
        {
            OverviewHighlight.Visibility = Visibility.Visible;
        }

        private void OverviewMouseLeave(object sender, MouseEventArgs e)
        {
            OverviewHighlight.Visibility = Visibility.Hidden;
        }

        private void OverviewClick(object sender, MouseEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Visible;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = 1;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = true;
            TradersMenuItem.IsChecked = false;
        }

        private void TradersMouseEnter(object sender, MouseEventArgs e)
        {
            TradersHighlight.Visibility = Visibility.Visible;
        }

        private void TradersMouseLeave(object sender, MouseEventArgs e)
        {
            TradersHighlight.Visibility = Visibility.Hidden;
        }

        private void TradersClick(object sender, MouseEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Visible;

            OverviewImage.Opacity = .5;
            TradersImage.Opacity = 1;

            OverviewMenuItem.IsChecked = false;
            TradersMenuItem.IsChecked = true;
        }


        private void onProcessStarted(object sender, RoutedEventArgs e)
        {
            OverviewImage.IsEnabled = false;
            TradersImage.IsEnabled = false;

            customize.IsEnabled = false;
        }

        private void onProcessFinished(object sender, RoutedEventArgs e)
        {
            OverviewImage.IsEnabled = true;
            TradersImage.IsEnabled = true;

            customize.IsEnabled = true;
        }


        #endregion
        #region Menu Commands

        private void cmdShowOverview(object sender, RoutedEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Visible;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = 1;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = true;
            TradersMenuItem.IsChecked = false;
        }

        private void cmdShowTraders(object sender, RoutedEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Visible;

            OverviewImage.Opacity = .5;
            TradersImage.Opacity = 1;

            OverviewMenuItem.IsChecked = false;
            TradersMenuItem.IsChecked = true;
        }

        private void cmdFraudDetection(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.FraudDetection)
            {
                Properties.Settings.Default.FraudDetection = false;
                //FraudDetectionMenuItem.IsChecked = false;
            }
            else
            {
                Settings.Fraud fraudWindow = new Settings.Fraud();
                fraudWindow.Left = this.Left + 55;
                fraudWindow.Top = this.Top + 35;
                fraudWindow.ShowDialog();
                if (Properties.Settings.Default.FraudDetection)
                {
                    FraudDetectionMenuItem.IsChecked = true;
                }
                else
                {
                    FraudDetectionMenuItem.IsChecked = false;
                }
            }
        }

        private void cmdShowAbout(object sender, RoutedEventArgs e)
        {
            customizeMenu.Visibility = Visibility.Hidden;
            aboutWindow.Left = this.Left + 55;
            aboutWindow.Top = this.Top + 35;

            aboutWindow.ShowDialog();
        }

        #endregion
        #region Data

        public class Game
        {
            public bool Active, Scheduled, Deleted;
            public int Traders { get; set; }
            public int DupeCount { get; set; }
            public int ProxyCount { get; set; }
            public string Name { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Directory { get; set; }
            public string Notes { get; set; }

            public Game()
            {
                Active = true;
                Scheduled = false;
                Deleted = false;
                Traders = 0;
                DupeCount = 0;
                ProxyCount = 0;
            }
        }

        public class Trader
        {
            public DateTime TimeStamp { get; set; }
            public bool Active { get; set; }
            public bool UsingProxy { get; set; }
            public bool IsDupe { get; set; }
            public int AddressCount { get; set; }
            public int UserID { get; set; }
            public string DisplayName { get; set; }
            public string Game { get; set; }
            public string Logon { get; set; }
            public string Alias { get; set; }
            public string LastIP { get; set; }
            public string Location { get; set; }
            public string Provider { get; set; }
            public string Note { get; set; }

            public Trader()
            {
                Active = true;
            }
        }

        public class Provider
        {
            public DateTime TimeStamp { get; set; }
            public bool Active { get; set; }
            public bool IsProxy { get; set; }
            public string Name { get; set; }
            public string IP { get; set; }
            public string Location { get; set; }

            public Provider(string name)
            {
                Name = name;
            }
        }

        public class Address
        {
            public bool Active { get; set; }
            public DateTime TimeStamp { get; set; }
            public string Logon { get; set; }
            public string Game { get; set; }
            public string IP { get; set; }

            public Address()
            {
                Active = true;
            }
        }



        #endregion
        #region Background Workers

        private void logWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
        }

        private void logWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //OverviewControl.Refresh();
            //OverviewControl.gamesDataGrid.IsEnabled = true;
            OverviewControl.gamesDataGrid.ItemsSource = GameList;

            //TradersControl.Refresh();
            //TradersControl.tradersDataGrid.IsEnabled = true;
            TradersControl.tradersDataGrid.ItemsSource = TraderList;

            if (Properties.Settings.Default.FraudDetection == true)
            {
                fraudWorker.RunWorkerAsync();
            }
         }


        private void logWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            foreach(Game game in GameList)
            {
                if(File.Exists(game.Directory + "\\twgame.log"))
                {
                    String line1, line2;
                    StreamReader logFile = new System.IO.StreamReader(game.Directory + "\\twgame.log");
                    while ((line1 = logFile.ReadLine()) != null)
                    {
                        if (line1.Contains("TWGame log initialized"))
                        {
                            // TODO: Parse log date
                        }

                        if (line1.Contains("New Player on Trade Wars"))
                        {
                            if ((line2 = logFile.ReadLine()) != null)
                            {
                                string display;
                                int uid = 0;

                                try
                                {
                                    string[] parts = line1.Split('(');
                                    string[] IP = line1.Split(' ')[3].Split('.');
                                    string alias = line2.Replace("Under the assumed name of ", "");
                                    string logon = parts[0].Substring(line1.IndexOf(' ', 23) + 1);
                                    uid = int.Parse(parts[1].Replace("): New Player on Trade Wars", ""));

                                    string address;
                                    if (IP.Count() > 2) address = $"{IP[0]}.{IP[1]}.{IP[2]}.0/24";
                                    else address = line1.Split(' ')[3];

                                    if (alias != "")
                                    {
                                        if (alias.ToLower() == logon.ToLower())
                                        {
                                            //display = string.Format("{0} ({1})", alias, uid);
                                            display = $"{alias} ({uid})";
                                        }
                                        else
                                        {
                                            //display = string.Format("{0}/{1} ({2})", logon, alias, uid);
                                            display = $"{logon} / {alias} ({uid})";
                                        }

                                        AddressList.Add(new Address()
                                        {
                                            TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                            Logon = logon,
                                            Game = game.Name,
                                            IP = address
                                        });

                                        TraderList.Add(new Trader()
                                        {
                                            TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                            UserID = uid,
                                            Game = game.Name,
                                            LastIP = line1.Split(' ')[3],
                                            Logon = logon,
                                            Alias = alias,
                                            DisplayName = display,
                                            AddressCount = 1
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // TODO: Log parse exception
                                }


                            }

                        }

                        if (line1.Contains("Ran Tradewars 2002."))
                        {
                            int uid = 0;

                            try
                            {
                                string[] parts = line1.Split('(');
                                string[] IP = line1.Split(' ')[3].Split('.');
                                string logon = parts[0].Substring(line1.IndexOf(' ', 23) + 1);

                                string address;
                                if (IP.Count() > 2) address = $"{IP[0]}.{IP[1]}.{IP[2]}.0/24";
                                else address = line1.Split(' ')[3];

                                uid = int.Parse(parts[1].Replace("): Ran Tradewars 2002.", ""));

                                List<Trader> traders = TraderList.Where(t => t.Game == game.Name & t.Logon == logon & t.LastIP != line1.Split(' ')[3]).ToList();
                                if (traders.Count() > 0)
                                {
                                    traders.Single().LastIP = line1.Split(' ')[3];

                                    if (AddressList.Where(a => a.Game == game.Name & a.Logon == logon & a.IP == address).Count() == 0)
                                    {
                                        AddressList.Add(new Address(){
                                            TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                            Logon = logon,
                                            Game = game.Name,
                                            IP = address});

                                        traders.Single().AddressCount ++;

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // TODO: Log parse exception
                            }


                        }

                    }

                    logFile.Close();

                    //var dupes = AddressList.Where(a => a.Game == game.Name).GroupBy(u => new { u.Game, u.IP }, u => u.IP).Distinct();
                        
                        //.Where(u => u.Count() > 1);
                }
            }


            foreach (Game g in GameList)
            {
                var dupes = AddressList.Where(u => u.Game == g.Name).GroupBy(u => u.IP, u => new { u.Logon, u.IP }).Where(u => u.Count() > 1);

                g.Traders = TraderList.Where(u => u.Game == g.Name).Count();
                g.DupeCount = dupes.Count();

                foreach (var dupe in dupes)
                {
                    string DupeLogons = "";
                    foreach (var item in dupe)
                    {
                        DupeLogons += $"{item.Logon}/";
                    }

                    foreach (var item in dupe)
                    {
                        Trader trader = TraderList.Where(t => t.Game == g.Name & t.Logon == item.Logon).Single();
                        trader.IsDupe = true;
                        trader.Note = $"Dupe: {DupeLogons} @ {item.IP}";
                    }
                }
            }
            if (Properties.Settings.Default.FraudDetection == false)
            {
                ReadProviderLog();
            }


        }

        private void ReadProviderLog()
        {
        }

        private void fraudWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
        }

        private void fraudWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private void fraudWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            ReadProviderLog();
        }

        #endregion

    }
}

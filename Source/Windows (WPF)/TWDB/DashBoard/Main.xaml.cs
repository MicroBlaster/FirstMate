using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml;
using System.Windows.Data;
using System.Threading;
using System.Timers;
using NetFwTypeLib;

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
        public List<Activity> ActivityList = new List<Activity>();

        // Lock Object for Threaded list changes
        private object lockActivity = new object();
        private object lockGame = new object();
        private object lockTrader = new object();

        // Background Worker
        BackgroundWorker logWorker;
        BackgroundWorker fraudWorker;

        // Refresh Timer
        System.Timers.Timer RefreshTimer;
        DateTime LastRefresh;

        // ProxyType names and colors
        static string[] ProxyName = { "None", "HTTP/Socks", "VPN", "Tor" };
        static string[] ProxyColor = { "Yellow", "Pink", "Pink", "Magenta"};

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
            //GamesControl.MainWindow = this;
            TradersControl.MainWindow = this;

            fraudWorker = new BackgroundWorker();
            fraudWorker.WorkerSupportsCancellation = true;
            fraudWorker.WorkerReportsProgress = true;
            fraudWorker.ProgressChanged += fraudWorkerProgress;
            fraudWorker.RunWorkerCompleted += fraudWorkerCompleted;
            fraudWorker.DoWork += fraudWorkerDoWorkAsync;

            logWorker = new BackgroundWorker();
            logWorker.WorkerSupportsCancellation = true;
            logWorker.WorkerReportsProgress = true;
            logWorker.ProgressChanged += logWorkerProgress;
            logWorker.RunWorkerCompleted += logWorkerCompleted;
            logWorker.DoWork += logWorkerDoWork;


            BindingOperations.EnableCollectionSynchronization(ActivityList, lockActivity);
            BindingOperations.EnableCollectionSynchronization(TraderList, lockTrader);

            GamesControl.gamesDataGrid.ItemsSource = GameList;
            TradersControl.tradersDataGrid.ItemsSource = TraderList;

            // TODO: add resresh on startup to config
            BeginRefresh();

            // TODO: add autorefresh and refresh time to config
            //RefreshTimer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            //RefreshTimer.AutoReset = true;
            RefreshTimer = new System.Timers.Timer(300000); // 5 Min
            RefreshTimer.Elapsed += RefreshTimerElapsed;
            RefreshTimer.AutoReset = true;
            RefreshTimer.Enabled = true;


        }

        private void RefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => BeginRefresh());
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
            if (Version == "")
            {
                System.Windows.MessageBox.Show("TradeWars Game Server does not appear to be installed.\nPlease run from the same machine as TWGS.", "Error");
                return;
            }

            ServerRoot = (string)Registry.GetValue(regkey, "ServerRoot", "");
            string TWName = (string)Registry.GetValue(regkey, "TWName", "");  // v1 BBS name - not used in v2
            string RegHost = (string)Registry.GetValue(regkey, "RegHost", "");
            string IPAddr = (string)Registry.GetValue(regkey, "IPAddr", "");
            string Port = (string)Registry.GetValue(regkey, "Port", "");
            string AdminPort = (string)Registry.GetValue(regkey, "AdminPort", "");
            string AdminPassword = (string)Registry.GetValue(regkey, "AdminPassword", "");

            if (Version.Contains("v1"))
            {
                OverviewControl.OverviewTextBlock1.Text = $"TradeWars Game Server {Version}\n Host Name: {RegHost}\nAddress: {IPAddr}\nGame Port: {Port}\nAdmin Port: {AdminPort}";
                OverviewControl.OverviewTextBlock2.Text = "";
            }
            else
            {
                string JumpgateTitle = (string)Registry.GetValue(regkey, "JumpgateTitle", "");
                string JumpgateAddress = (string)Registry.GetValue(regkey, "JumpgateAddress", "");
                string JumpgatePort = (string)Registry.GetValue(regkey, "JumpgatePort", "");
                string JumpgateEmail = (string)Registry.GetValue(regkey, "JumpgateEmail", "");
                string JumpgateWebsite = (string)Registry.GetValue(regkey, "JumpgateWebsite", "");
                string JumpgateNotes = (string)Registry.GetValue(regkey, "JumpgateNotes", "");
                string JumpgateActive = (string)Registry.GetValue(regkey, "JumpgateActive", "");
                string JumpgatePublic = (string)Registry.GetValue(regkey, "JumpgatePublic", "");

                // TODO: Validate Jumpgate Settings
                // TODO: Configure / Activate Jumpgate

                OverviewControl.OverviewTextBlock1.Text = $"TradeWars Game Server {Version}\nHost Name: {RegHost}\nAddress: {IPAddr}\nGame Port: {Port}\nAdmin Port: {AdminPort}";
                if (JumpgateActive == "T")
                {
                    if (JumpgatePublic == "T")
                    {
                        OverviewControl.OverviewTextBlock2.Text = $"Jumpgate Listing: Active / Public\nTitle: {JumpgateTitle}\nAddress: {JumpgateAddress}\nPort: {JumpgatePort}\nEmail: {JumpgateEmail}\nWebsite: {JumpgateWebsite}";
                    }
                    else
                    {
                        OverviewControl.OverviewTextBlock2.Text = $"Jumpgate Listing: Private (Not visible on Website)\nTitle: {JumpgateTitle}\nEmail: {JumpgateEmail}\nWebsite: {JumpgateWebsite}";
                    }
                }
                else
                {
                    OverviewControl.OverviewTextBlock2.Text = "Jumpgate Listing: Inactive";
                }
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

        private void onRefreshMouseEnter(object sender, MouseEventArgs e)
        {
            if(ProgressBorder.Visibility == Visibility.Hidden)
            {
                RefreshImage.Opacity = .5;
            }
        }

        private void onRefreshMouseLeave(object sender, MouseEventArgs e)
        {
            if (ProgressBorder.Visibility == Visibility.Hidden)
            {
                RefreshImage.Opacity = .3;
            }
            else
            {
                RefreshImage.Opacity = .1;
            }
        }

        private void onRefreshClick(object sender, MouseButtonEventArgs e)
        {
            BeginRefresh();
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
            GamesControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = 1;
            GamesImage.Opacity = .5;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = true;
            GamesMenuItem.IsChecked = false;
            TradersMenuItem.IsChecked = false;
        }

        private void GamesMouseEnter(object sender, MouseEventArgs e)
        {
            GamesHighlight.Visibility = Visibility.Visible;
        }

        private void GamesMouseLeave(object sender, MouseEventArgs e)
        {
            GamesHighlight.Visibility = Visibility.Hidden;
        }

        private void GamesClick(object sender, MouseEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Hidden;
            GamesControl.Visibility = Visibility.Visible;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = .5;
            GamesImage.Opacity = 1;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = false;
            GamesMenuItem.IsChecked = true;
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
            GamesControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Visible;

            OverviewImage.Opacity = .5;
            GamesImage.Opacity = .5;
            TradersImage.Opacity = 1;

            OverviewMenuItem.IsChecked = false;
            GamesMenuItem.IsChecked = false;
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
            GamesControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = 1;
            GamesImage.Opacity = .5;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = true;
            GamesMenuItem.IsChecked = false;
            TradersMenuItem.IsChecked = false;
        }

        private void cmdShowGames(object sender, RoutedEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Hidden;
            GamesControl.Visibility = Visibility.Visible;
            TradersControl.Visibility = Visibility.Hidden;

            OverviewImage.Opacity = .5;
            GamesImage.Opacity = 1;
            TradersImage.Opacity = .5;

            OverviewMenuItem.IsChecked = false;
            GamesMenuItem.IsChecked = true;
            TradersMenuItem.IsChecked = false;
        }

        private void cmdShowTraders(object sender, RoutedEventArgs e)
        {
            OverviewControl.Visibility = Visibility.Hidden;
            GamesControl.Visibility = Visibility.Hidden;
            TradersControl.Visibility = Visibility.Visible;

            OverviewImage.Opacity = .5;
            GamesImage.Opacity = .5;
            TradersImage.Opacity = 1;

            OverviewMenuItem.IsChecked = false;
            GamesMenuItem.IsChecked = false;
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
                    fraudWorker.RunWorkerAsync();
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

        public class Activity
        {
            public bool Bannable { get; set; }
            public DateTime TimeStamp { get; set; }
            public string Value { get; set; }
            public string Background { get; set; }
            public string Address { get; set; }

            public Activity() { }
        }

        public class Game
        {
            public bool Active, Scheduled, Deleted;
            public int Traders { get; set; }
            public int DupeCount { get; set; }
            public int FraudCount { get; set; }
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
                FraudCount = 0;
            }
        }

        public class Trader
        {
            public DateTime TimeStamp { get; set; }
            public bool Active { get; set; }
            public bool Banned { get; set; }
            public bool IsDupe { get; set; }
            public bool IsFraud { get; set; }
            public bool IsDynamic { get; set; }  // TODO: Test for Static / Dynamic IPs
            public int ProxyType { get; set; }
            public int AddressCount { get; set; }
            public int UserID { get; set; }
            public int IpqFraudScore { get; set; }
            public int IpiFraudScore { get; set; }
            public string DisplayName { get; set; }
            public string DisplayAddress { get; set; }
            public string LastIP { get; set; }
            public string Game { get; set; }
            public string Logon { get; set; }
            public string Alias { get; set; }
            public string Location { get; set; }
            public string Provider { get; set; }
            public string Note { get; set; }
            public string LastError { get; set; }

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

        private void BeginRefresh()
        {
            if(ProgressBorder.Visibility == Visibility.Hidden)
            {
                ProgressBar.Value = 0;
                ProgressLabel.Content = "Scanning...";
                ProgressBorder.Visibility = Visibility.Visible;
                RefreshImage.Opacity = .1;

                AddressList.Clear();
                ActivityList.Clear();

                // Clear the notes field in TraderList for all records.
                TraderList.Where(t => t.Note != null).ToList().ForEach(t => t.Note = null);

                // Clear the LastIP field in TraderList for all records.
                // TraderList.ForEach(t => t.LastIP = null);


                logWorker.RunWorkerAsync();
            }
        }

        private void RefrehCompleted()
        {
            foreach (Game g in GameList)
            {
                g.FraudCount = TraderList.Where(t => t.Game == g.Name & t.IsFraud).Count();
                g.ProxyCount = TraderList.Where(t => t.Game == g.Name & t.ProxyType > 0).Count();
            }

            OverviewControl.ActivityDataGrid.ItemsSource = ActivityList.OrderByDescending(a => a.TimeStamp).Take(100);
            OverviewControl.Refresh();

            GamesControl.gamesDataGrid.ItemsSource = GameList;
            GamesControl.Refresh();

            TradersControl.tradersDataGrid.ItemsSource = TraderList.ToList();
            TradersControl.Refresh();

            ProgressBorder.Visibility = Visibility.Hidden;
            RefreshImage.Opacity = .3;

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            INetFwRule rule = null;
            try
            {
                rule = firewallPolicy.Rules.Item("TW2002 Banned Traders");
            }
            catch { }

            if (rule != null)
            {
                string[] remoteAddresses = rule.RemoteAddresses.Split(',');

                foreach(string address in remoteAddresses)
                {
                    string s = address.Replace(".0/255.255.255.0", "");
                    foreach(Trader trader in TraderList)
                    {
                        if(trader.LastIP.Contains(s))
                        {
                            trader.Banned = true;
                        }
                    }
                }
            }

        }


        private void logWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgressLabel.Content = (string) e.UserState;
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void logWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (Properties.Settings.Default.FraudDetection == true)
            {
                OverviewControl.ActivityDataGrid.ItemsSource = ActivityList.OrderByDescending(a => a.TimeStamp).Take(100);
                OverviewControl.Refresh();

                fraudWorker.RunWorkerAsync();
            }
            else
            {
                RefrehCompleted();
            }
        }

        private void logWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            DateTime detected = new DateTime();
            int CurrentGame = 0;
            int GameCount = GameList.Count();

            foreach (Game game in GameList)
            {
                CurrentGame++;
                logWorker.ReportProgress((CurrentGame * 100 / GameCount) / 4 * 3, $"Game {game.Name} Logs...");

                //System.Threading.Thread.Sleep(10000);

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
                                    if (IP.Count() > 2) address = $"{IP[0]}.{IP[1]}.{IP[2]}.*";
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

                                        lock (lockTrader)
                                        {
                                            List<Trader> traders = TraderList.Where(t => t.Game == game.Name & t.Logon == logon).ToList();
                                            if (traders.Count() == 0)
                                            {
                                                TraderList.Add(new Trader()
                                                {
                                                    TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                                    UserID = uid,
                                                    Game = game.Name,
                                                    LastIP = line1.Split(' ')[3],
                                                    DisplayAddress = line1.Split(' ')[3],
                                                    Logon = logon,
                                                    Alias = alias,
                                                    DisplayName = display,
                                                    AddressCount = 1
                                                });

                                            }
                                            else
                                            {
                                                // Updateing LastIP during refresh prevents false IP Change detection
                                                traders.Single().LastIP = line1.Split(' ')[3];
                                            }
                                        }

                                        lock (lockActivity)
                                        {
                                            ActivityList.Add(new Activity()
                                            {
                                                TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                                Value = $"New Trader in Game {game.Name} - {display} from {line1.Split(' ')[3]}",
                                                Address = line1.Split(' ')[3],
                                                Bannable = true
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ActivityList.Add(new Activity()
                                    {
                                        TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                        Value = $"Unable to parse line: {line1.Substring(22)}",
                                        Background = "LightYellow",
                                        Address = line1.Split(' ')[3],
                                        Bannable = true
                                    });
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
                                if (IP.Count() > 2) address = $"{IP[0]}.{IP[1]}.{IP[2]}.*";
                                else address = line1.Split(' ')[3];

                                uid = int.Parse(parts[1].Replace("): Ran Tradewars 2002.", ""));

                                List<Trader> traders = TraderList.Where(t => t.Game == game.Name & t.Logon == logon & t.LastIP != line1.Split(' ')[3]).ToList();
                                if (traders.Count() > 0)
                                {
                                    lock (lockActivity)
                                    {
                                        ActivityList.Add(new Activity()
                                        {
                                            TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                            Value = $"Trader {traders.Single().DisplayName} Changed address from {traders.Single().LastIP} to {line1.Split(' ')[3]} in Game {game.Name}",
                                            Background = "LightYellow",
                                            Address = line1.Split(' ')[3],
                                            Bannable = true
                                        });
                                    }

                                    traders.Single().LastIP = line1.Split(' ')[3];
                                    traders.Single().TimeStamp = Convert.ToDateTime(line1.Substring(0, 22));

                                    if (AddressList.Where(a => a.Game == game.Name & a.Logon == logon & a.IP == address).Count() == 0)
                                    {
                                        AddressList.Add(new Address()
                                        {
                                            TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                            Logon = logon,
                                            Game = game.Name,
                                            IP = address
                                        });

                                        traders.Single().AddressCount++;
                                        traders.Single().IsDynamic = true;
                                        traders.Single().DisplayAddress = $"{line1.Split(' ')[3]} ({traders.Single().AddressCount})";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (lockActivity)
                                {
                                    ActivityList.Add(new Activity()
                                    {
                                        TimeStamp = Convert.ToDateTime(line1.Substring(0, 22)),
                                        Value = $"Unable to parse line: {line1.Substring(22)}",
                                        Background = "LightYellow",
                                    });
                                }
                            }

                        }

                    }

                    logFile.Close();

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

                    detected = DateTime.MinValue;
                    foreach (var item in dupe)
                    {
                        Trader trader = TraderList.Where(t => t.Game == g.Name & t.Logon == item.Logon).First();
                        trader.IsDupe = true;
                        trader.Note = $"Dupe: {DupeLogons} @ {item.IP}".Replace("/ ", " ");
                        detected = trader.TimeStamp > detected ? trader.TimeStamp : detected;
                    }

                    lock (lockActivity)
                    {
                        ActivityList.Add(new Activity()
                        {
                            TimeStamp = detected.AddSeconds(1),
                            Value = $"Duplicate Player detected in Game {g.Name} - {DupeLogons} @ {dupe.First().IP}".Replace("/ ", " "),
                            Background = "Red",
                            Address = dupe.First().IP,
                            Bannable = true
                        });
                    }
                }
            }

            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            if(File.Exists(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log")))
            { 
                XmlDocument xmlDoc = new XmlDocument();
                string line = "";

                using (StreamReader sr = new StreamReader(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log")))
                {
                    while (sr.Peek() >= 0)
                    {

                        try
                        {
                            line = sr.ReadLine();

                            xmlDoc.LoadXml(line);
                            XmlNode providerNode = xmlDoc.SelectSingleNode("//Provider");

                            detected = DateTime.MinValue;
                            string LastIP = providerNode.Attributes["IP"].Value;

                            int IpqFraudScore = Int32.Parse(providerNode.Attributes["IpqFraudScore"].Value);
                            int IpiFraudScore = (Int32)(Double.Parse(providerNode.Attributes["IpiFraudScore"].Value));

                            List<Trader> traders = TraderList.Where(t => t.LastIP == LastIP).ToList();
                            string TraderNames = "";
                            foreach (Trader trader in traders)
                            {
                                trader.LastError = providerNode.Attributes["LastError"].Value;
                                trader.Provider = providerNode.Attributes["Provider"].Value;
                                trader.Location = providerNode.Attributes["Location"].Value;
                                trader.IsFraud = providerNode.Attributes["IsFraud"].Value == "True";
                                trader.IpqFraudScore = IpqFraudScore;
                                trader.IpiFraudScore = IpiFraudScore;

                                trader.ProxyType = 0;
                                if (providerNode.Attributes["ProxyType"] != null)
                                    trader.ProxyType = Int32.Parse(providerNode.Attributes["ProxyType"].Value);

                                if (trader.IsFraud)
                                {
                                    trader.Note = $"Fraud Score: {trader.IpqFraudScore} / {trader.IpiFraudScore} Proxy: {ProxyName[traders.First().ProxyType]} {trader.Note}";
                                    TraderNames += $"{trader.Logon}/";
                                    detected = trader.TimeStamp > detected ? trader.TimeStamp : detected;
                                }
                            }
                            if (traders.Count > 0)
                            {
                                if (traders.First().IsFraud)
                                {
                                    lock (lockActivity)
                                    {
                                        ActivityList.Add(new Activity()
                                        {
                                            TimeStamp = detected.AddSeconds(1),
                                            Value = $"Suspicious provider detected @ {LastIP} Fraud Score: {IpqFraudScore} / {IpiFraudScore} Proxy: {ProxyName[traders.First().ProxyType]} Traders: {TraderNames.Replace("/", "")}",
                                            Background = ProxyColor[traders.First().ProxyType],
                                            Address = traders.First().LastIP,
                                            Bannable = true
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (lockActivity)
                            {
                                ActivityList.Add(new Activity()
                                {
                                    TimeStamp = detected.AddSeconds(1),
                                    Value = $"Error reading provider log: {line}",
                                    Background = "Pink"
                                });
                            }
                        }
                    }
                }
            }
        }


        private static async Task CheckFraud(Trader trader)
        {
            HttpClient client = new HttpClient();
            trader.LastError = null;

            try
            {
                string Uri1 = "https://www.ipqualityscore.com/api/xml/ip";
                string Uri2 = "http://check.getipintel.net/check.php";
                string Email = Properties.Settings.Default.EmailAddress;
                string Key = Properties.Settings.Default.PrivateKey;

                // Initiate ipqualityscore.com request in the background.
                // var stringTask1 = client.GetStringAsync($"{Uri1}/{Key}/{trader.LastIP}");
                var stringTask1 = client.GetStringAsync($"{Uri1}/{Key}/{trader.LastIP}?strictness=4");

                // Initiate getipintel.net request in the background.
                // var stringTask2 = client.GetStringAsync($"{Uri2}?ip={trader.LastIP}&contact={Email}&flags=m");
                var stringTask2 = client.GetStringAsync($"{Uri2}?ip={trader.LastIP}&contact={Email}&flags=b");


                // Process ipqualityscore.com result.
                var msg = await stringTask1;
                XmlDocument xmlDoc2 = new XmlDocument();
                xmlDoc2.LoadXml(msg.Replace("\n", "").Replace("\t", ""));
                XmlNode resultNode = xmlDoc2.SelectSingleNode("//result");
                if (resultNode["success"].FirstChild.Value == "true")
                {
                    String city = resultNode["city"].FirstChild.Value;
                    String region = resultNode["region"].FirstChild.Value;
                    String country = resultNode["country_code"].FirstChild.Value;

                    trader.ProxyType = 0;
                    if (resultNode["proxy"].FirstChild.Value == "true") trader.ProxyType = 1;
                    if (resultNode["vpn"].FirstChild.Value == "true") trader.ProxyType = 2;
                    if (resultNode["tor"].FirstChild.Value == "true") trader.ProxyType = 3;
                    //bool abuse = (resultNode["recent_abuse"].FirstChild.Value == "true");

                    trader.IpqFraudScore = Int32.Parse(resultNode["fraud_score"].FirstChild.Value);
                    trader.Provider = resultNode["ISP"].FirstChild.Value.Replace("&", "");  // or use organization?
                    trader.Location = $"{city}, {region} {country}";
                    trader.IsFraud = (trader.IpqFraudScore > 0 | trader.ProxyType > 0);

                }
                else
                {
                    trader.LastError = resultNode["message"].FirstChild.Value;
                }

                try
                {
                    //Process ipqualityscore.com result.
                    msg = await stringTask2;
                    trader.IpiFraudScore = (Int32)(Double.Parse(msg) * 100);
                    trader.IsFraud = (trader.IsFraud | trader.IpiFraudScore > 0);

                    //The proxy check system will return negative values on error.For standard format(non - json), an additional HTTP 400 status code is returned
                    //- 1 Invalid no input
                    //- 2 Invalid IP address
                    //- 3 Unroutable address / private address
                    //-4 Unable to reach database, most likely the database is being updated.Keep an eye on twitter for more information.
                    //-5 Your connecting IP has been banned from the system or you do not have permission to access a particular service. Did you exceed your query limits? Did you use an invalid email address? If you want more information, please use the contact links below.
                    //-6 You did not provide any contact information with your query or the contact information is invalid.
                    //If you exceed the number of allowed queries, you'll receive a HTTP 429 error.
                }
                catch (Exception)
                {
                    trader.IpiFraudScore = -100;
                }
            }
            catch (Exception ex)
            {
                trader.LastError = ex.Message;
            }
        
            if (trader.IsFraud)
            {
                trader.Note = $"Fraud Score: {trader.IpqFraudScore} / {trader.IpiFraudScore} Proxy: {ProxyName[trader.ProxyType]} {trader.Note}";
            }


        }

        private void fraudWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgressLabel.Content = (string)e.UserState;
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void fraudWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RefrehCompleted();          
        }

        private void fraudWorkerDoWorkAsync(object sender, DoWorkEventArgs e)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!Directory.Exists($"{AppDataPath}/TradeWars/Dashboard"))
            {
                Directory.CreateDirectory($"{AppDataPath}/TradeWars");
                Directory.CreateDirectory($"{AppDataPath}/TradeWars/Dashboard");
            }

            using (StreamWriter outputFile = new StreamWriter(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log"), true))
            {
                List<Trader> traders = TraderList.Where(t => t.Location == null && t.LastError == null).OrderBy(t => t.LastIP).Take(10).ToList();

                int CurrentTrader = 0;
                int TraderCount = traders.Count();

                if (LastRefresh.AddMinutes(2) > DateTime.Now | TraderCount == 0)
                {
                    fraudWorker.ReportProgress(110, "Completed...");
                    System.Threading.Thread.Sleep(1000);
                    return;
                }


                Trader LastTrader = new Trader();
                foreach (Trader trader in traders)
                {
                    LastRefresh = DateTime.Now;

                    CurrentTrader++;
                    fraudWorker.ReportProgress(((CurrentTrader * 100 / TraderCount) / 4) + 75, $"IP: {trader.LastIP}");
                    System.Threading.Thread.Sleep(500);

                    if (LastTrader.LastIP != trader.LastIP)
                    {
                        LastTrader = trader;

                        CheckFraud(trader).Wait();

                        outputFile.WriteLine($"<Provider IP=\"{trader.LastIP}\" LastError=\"{trader.LastError}\" Provider=\"{trader.Provider}\" Location=\"{trader.Location}\" IsFraud=\"{trader.IsFraud}\" ProxyType=\"{trader.ProxyType}\" IpqFraudScore=\"{trader.IpqFraudScore}\" IpiFraudScore=\"{trader.IpiFraudScore}\" Checked=\"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}\" />");

                        if (trader.LastError != null)
                        {
                            trader.Note = $"Fraud Check Failed. {trader.Note}";
                            lock (lockActivity)
                            {
                                ActivityList.Add(new Activity()
                                {
                                    TimeStamp = trader.TimeStamp.AddSeconds(1),
                                    Value = $"Fraud Check Failed for {trader.LastIP} Trader: {trader.DisplayName}",
                                    Background = "Pink",
                                });
                            }
                        }
                        else
                        {
                            if (trader.IsFraud)
                            {
                                lock (lockActivity)
                                {
                                    ActivityList.Add(new Activity()
                                    {
                                        TimeStamp = trader.TimeStamp.AddSeconds(1),
                                        Value = $"Suspicious provider detected @ {trader.LastIP} Fraud Score: {trader.IpqFraudScore} / {trader.IpiFraudScore} Proxy: {ProxyName[trader.ProxyType]} Trader: {trader.Logon}",
                                        Background = ProxyColor[trader.ProxyType],
                                        Address = trader.LastIP,
                                        Bannable = true
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // Skip Fraud Check, becuase this is a Duplicate and copy Fraud info from last trader.
                        trader.LastError = LastTrader.LastError;
                        trader.Provider = LastTrader.Provider;
                        trader.Location = LastTrader.Location;
                        trader.IsFraud = LastTrader.IsFraud;
                        trader.IpqFraudScore = LastTrader.IpqFraudScore;
                        trader.IpiFraudScore = LastTrader.IpiFraudScore;
                    }

                }
            }
        }

        #endregion

    }
}

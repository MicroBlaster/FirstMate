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

            //OverviewControl.MainWindow = this;
            //GamesControl.MainWindow = this;
            //TradersControl.MainWindow = this;

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

        public class Game
        {
            public bool Active, Scheduled, Deleted;
            public int Traders { get; set; }
            public int DupeCount { get; set; }
            public int FraudCount { get; set; }
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
            public bool IsDupe { get; set; }
            public bool IsFraud { get; set; }
            public bool IsDynamic { get; set; }
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

        private void logWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
        }

        private void logWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (Properties.Settings.Default.FraudDetection == true)
            {
                fraudWorker.RunWorkerAsync();
            }
            else
            {
                foreach (Game g in GameList)
                {
                    g.FraudCount = TraderList.Where(t => t.Game == g.Name & t.IsFraud).Count();
                }

                GamesControl.gamesDataGrid.ItemsSource = GameList;
                TradersControl.tradersDataGrid.ItemsSource = TraderList;
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
                                            DisplayAddress = line1.Split(' ')[3],
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
                                    throw;
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
                                        traders.Single().IsDynamic = true;
                                        traders.Single().DisplayAddress = $"{line1.Split(' ')[3]} ({traders.Single().AddressCount})";

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // TODO: Log parse exception
                                throw;
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

                    foreach (var item in dupe)
                    {
                        Trader trader = TraderList.Where(t => t.Game == g.Name & t.Logon == item.Logon).Single();
                        trader.IsDupe = true;
                        trader.Note = $"Dupe: {DupeLogons} @ {item.IP}".Replace("/ "," ");
                    }
                }
            }

            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            if(File.Exists(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log")))
            { 
            XmlDocument xmlDoc = new XmlDocument();

                using (StreamReader sr = new StreamReader(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log")))
                {
                    while (sr.Peek() >= 0)
                    {
                        try
                        {
                            string line = sr.ReadLine();
                            xmlDoc.LoadXml(line);
                            XmlNode providerNode = xmlDoc.SelectSingleNode("//Provider");

                            string LastIP = providerNode.Attributes["IP"].Value;

                            List<Trader> traders = TraderList.Where(t => t.LastIP == LastIP).ToList();
                            foreach (Trader trader in traders)
                            {
                                trader.LastError = providerNode.Attributes["LastError"].Value;
                                trader.Provider = providerNode.Attributes["Provider"].Value;
                                trader.Location = providerNode.Attributes["Location"].Value;
                                trader.IsFraud = (providerNode.Attributes["IsFraud"].Value == "True");
                                trader.IpqFraudScore = Int32.Parse(providerNode.Attributes["IpqFraudScore"].Value);
                                trader.IpiFraudScore = Int32.Parse(providerNode.Attributes["IpiFraudScore"].Value);

                                if (trader.IsFraud)
                                {
                                    trader.Note = $"Fraud Score: {trader.IpqFraudScore} / {trader.IpiFraudScore * 100} {trader.Note}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
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
                var stringTask1 = client.GetStringAsync($"{Uri1}/{Key}/{trader.LastIP}");

                // Initiate getipintel.net request in the background.
                var stringTask2 = client.GetStringAsync($"{Uri2}?ip={trader.LastIP}&contact={Email}&flags=m");


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

                    bool proxy = (resultNode["proxy"].FirstChild.Value == "true");
                    bool vpn = (resultNode["vpn"].FirstChild.Value == "true");
                    bool tor = (resultNode["tor"].FirstChild.Value == "true");
                    bool abuse = (resultNode["recent_abuse"].FirstChild.Value == "true");

                    trader.IpqFraudScore = Int32.Parse(resultNode["fraud_score"].FirstChild.Value);
                    trader.Provider = resultNode["ISP"].FirstChild.Value.Replace("&", "");  // or use organization?
                    trader.Location = $"{city}, {region} {country}";
                    trader.IsFraud = (proxy | vpn | tor | abuse | trader.IpqFraudScore > 0);

                }
                else
                {
                    trader.LastError = resultNode["message"].FirstChild.Value;
                }

                try
                {
                    //Process ipqualityscore.com result.
                    msg = await stringTask2;
                    trader.IpiFraudScore = Int32.Parse(msg);
                    trader.IsFraud = (trader.IsFraud | trader.IpiFraudScore > 0);
                }
                catch (Exception)
                {
                    trader.IpiFraudScore = -1;
                }
            }
            catch (Exception ex)
            {
                trader.LastError = ex.Message;
            }
        
            if (trader.IsFraud)
            {
                trader.Note = $"Fraud Score: {trader.IpqFraudScore} / {trader.IpiFraudScore * 100} {trader.Note}";
            }

        }

        private void fraudWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
        }

        private void fraudWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach(Game g in GameList)
            {
                g.FraudCount = TraderList.Where(t => t.Game == g.Name & t.IsFraud).Count();
            }

            GamesControl.gamesDataGrid.ItemsSource = GameList;
            TradersControl.tradersDataGrid.ItemsSource = TraderList;
        }

        private void fraudWorkerDoWorkAsync(object sender, DoWorkEventArgs e)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!Directory.Exists($"{AppDataPath}/TradeWars/Dashboard"))
            {
                Directory.CreateDirectory($"{AppDataPath}/TradeWars");
                Directory.CreateDirectory($"{AppDataPath}/TradeWars/Dashboard");
            }

            using (StreamWriter outputFile = new StreamWriter(Path.Combine($"{AppDataPath}/TradeWars/Dashboard", "provider.log"),true))
            {
                foreach (Trader trader in TraderList.Where(t => t.Location == null && t.LastError == null))
                {
                    CheckFraud(trader).Wait();
                    outputFile.WriteLine($"<Provider IP=\"{trader.LastIP}\" LastError=\"{trader.LastError}\" Provider=\"{trader.Provider}\" Location=\"{trader.Location}\" IsFraud=\"{trader.IsFraud}\" IpqFraudScore=\"{trader.IpqFraudScore}\" IpiFraudScore=\"{trader.IpiFraudScore}\" Checked=\"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortDateString()}\" />");
                    if (trader.LastError != null)
                    {
                        trader.Note = $"Fraud Check Failed. {trader.Note}";
                    }
                }
            }
        }

        #endregion

    }
}

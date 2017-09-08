using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FirstMate
{
    public class Session
    {
        private string banner;
        private List<Session> sessions;
        public List<Terminal.Server> Servers { get; private set; }
        public List<FirstMate.Client> Clients { get; private set; }
        public List<Menu> Menus { get; private set; }

        private static int nextID = 0;
        public int SessionID { get; private set; }

        private enum State { Offline, Online, Blocking, Menu };

        private State state = State.Offline;

        /// <summary>
        /// Occurs when client initialization is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public event EventHandler<EventArgs> Initialized = delegate { };


        public Session(Helper parrent)
        {
            sessions = parrent.Sessions;
            banner = parrent.Banner;

            Servers = new List<Terminal.Server>();
            Clients = new List<FirstMate.Client>();

            SessionID = nextID++;

            //client.Connect += onConnect;
            //client.Receive += onReceive;
            //client.Disconnect += onDisconnect;

            //ClientList.Add(client);


            Stream rs = typeof(Menu).GetTypeInfo().Assembly.GetManifestResourceStream("FirstMate.Resources.menu.xml");
//            Using(XmlSerializer xml = new XmlSerializer(typeof(List<Menu>), new XmlRootAttribute("Menus")));
//            {
//                Menus = (List<Menu>)xml.Deserialize(rs);
//            }




            // todo: add clientID

        }

        public void Start()
        {
            foreach (Terminal.Server server in Servers)
            {
                server.Start();
            }
        }

        public void Pause()
        {
            foreach (Terminal.Server server in Servers)
            {
                server.Stop();
            }
        }

        public void Stop()
        {
            foreach (Terminal.Server server in Servers)
            {
                server.Stop();
            }

            while (Clients.Count > 0)
            {
                Client client = Clients.First();
                Clients.Remove(client);
                client.Close();
            }
        }

        public void AddServer(Config.Connection connection, string banner)
        //Config.ConnectionType type, IPAddress ip, int port)
        {
            switch (connection.Type)
            {
                case Config.ConnectionType.Client:

                    // TODO: add client connections (i.e. Tavern Chat)
                    break;

                case Config.ConnectionType.Server:
                    Terminal.Server server = new Terminal.Server(connection.IP, connection.Port);
                    server.Banner = banner;
                    server.Connected += onConnected;
                    Servers.Add(server);
                    break;
            }
        }

        private void onConnected(object sender, EventArgs e)
        {
            TcpClient tcpClient = (TcpClient)sender;

            FirstMate.Client client = new FirstMate.Client(tcpClient, banner);

            client.Initialized += onInitialized;
            client.Receive += onReceive;
            client.Disconnect += onDisconnect;

            Clients.Add(client);

            //TODO: callback event to parrent for logging.
            //Console.Write("\rConnection accepted from " + client.RemoteEP.Address + ".\nDaemon:\\> ");
        }

        private void onInitialized(object sender, EventArgs e)
        {
            Client client = (Client)sender;
            //int sessionCount = SessionList.Where(session => session.state != State.Offline).Count();
            int sessionCount = sessions.GroupBy(session => session.SessionID).Distinct().Count() - 1;

            //FirstMate.Menu m = new FirstMate.Menu();

            // Send Welcome
            string s = string.Format(
                "\r\u001B[1;32m  Conection accepted...\r\n\r\n" +
                "  \u001B[0;35mHello \u001B[1;36m{0} \u001B[0;35m[\u001B[33;1m{1}\u001B[0;35m]\r\n\r\n",
                client.ReverseDNS, client.RemoteEP.Address);

            switch (sessionCount)
            {
                case 0:
                    s += "    \u001B[1;34mThere are \u001B[0;31mno\u001B[1;34m active game sessions.\r\n\r\n";
                    //s += "    \u001B[1;34mThere are no active game sessions.\r\n\r\n";
                    break;

                case 1:
                    s += "    \u001B[1;34mThere is \u001B[0;36m1\u001B[1;34m active game session.\r\n\r\n";
                    break;

                default:
                    s += "    \u001B[1;34mThere are \u001B[0;36m" + sessionCount + "\u001B[1;34m active game sessions.\r\n\r\n";
                    break;
            }

            if (sessionCount > 0)
            {
                s += "\u001B[0;32mType \u001B[1;36m~SL\u001B[0;32m to list all sessions,\r\n";
                s += "\u001B[0;32mType \u001B[1;36m~SJ\u001B[0;35m<\u001B[0;33mID\u001B[0;35m>\u001B[0;32m to join and existing session,\r\n";
            }

            s += "\u001B[0;32mType \u001B[1;36m~SN\u001B[0;32m to create a new session,\r\n" +
                 "\u001B[0;32mor Type \u001B[1;36m~?\u001B[0;32m for more information.\r\n\r\n" +
                 //"\u001B[0;31m¿habla español? \u001B[0;32mescriba \u001B[1;36m~L2\u001B[0;32m para cambiar a español.\r\n\r\n" +
                 "\u001B[0;36mFor the latest news, suppport, and updates visit us \n\r" +
                 "online at \u001B[0;35mhttps://github.com/MicroBlaster/FirstMate/wiki\u001B[0m\r\n\r\n";


            //"    Terminal Type   : {2}\n\r" +
            //"    Window Size     : {3} X {4}\n\r" +
            //"    ANSI Color      : {5}\n\r", 
            //        reverseDNS, remoteEP.Address,
            //        telnetReponse.TerminlaType,
            //        telnetReponse.WindowSizeX,
            //        telnetReponse.WindowSizeY,
            //        ansiDetected ? "Enabled":"Disabled");


            //TODO: Add localization to initialization (ClinetProxy mode only).
            client.Write(s);

            //TODO: Send hello, welcome, and login (GameServer mode only)



            // Start the clinet and begin receiving data.
            client.Start();

            //while (state != State.Offline)
            //{
            //    try
            //    {
            //        // Get input from a console connection
            //        byte[] readBuffer = new byte[1];
            //        int readBytes = stream.Read(readBuffer, 0, 1);
            //        //stream.ReadTimeout()

            //        if (readBytes > 0)
            //        {
            //            char key = (char)readBuffer[0];

            //            if ((state == State.Online))
            //            {
            //                if (key == '~')
            //                {
            //                    // Escape character received. begin blockinig session.
            //                    state = State.Blocking;

            //                    // Display Prompt
            //                    s = "\u001B[1;36m~\u001B[0;35mFirstMate\u001B[0;32m>";
            //                    byte[] prompt = Encoding.ASCII.GetBytes(s);
            //                    stream.Write(prompt, 0, prompt.Length);

            //                }
            //                else
            //                {
            //                    stream.Write(readBuffer, 0, readBytes);
            //                }
            //            }
            //            else // state = State.Blocking
            //            {
            //                switch (menus)
            //                {
            //                    case Menus.None:
            //                        switch(key)
            //                        {
            //                            case 'H':
            //                                menus = Menus.Help;
            //                                Menu.Help();
            //                                break;
            //                        }

            //                        break;

            //                    case Menus.Configuratiuon:

            //                        break;

            //                    case Menus.Help:

            //                        break;

            //                    case Menus.Identity:

            //                        break;

            //                    case Menus.Localization:

            //                        break;

            //                    case Menus.Session:

            //                        break;
            //                }

            //            }
            //        }


            //    }
            //    catch (Exception)
            //    { }
            //}

        }

        private void onReceive(object sender, EventArgs e)
        {
            Terminal.Client client = (Terminal.Client)sender;
            //private string data = (String)sender;

            //List <Global> globals = new List<Global>();

            //globals.Add(new Global('~', "FirstMate (?=Help)>"));
            //globals.Add(new Global('.', "SBot {mic}>"));

            //foreach (char c in client.Data)
            //{
            //    if (globals.Where(g => g.Key == c).Any())
            //    {
            //        Global global = globals.Where(g => g.Key == c).First();

            //        client.Write("\r\n" + global.Prompt);
            //    }

            //}

            //char[] globals = new char[] { '~', '.' };
            //string[] globalMenus = new string[] { "FirstMate", "SBOT" };

                //foreach (char c in client.Data)
                //{
                //    if (client.Blocking)
                //    {

                //    }
                //    else
                //    {
                //        if (globals.Contains(c))
                //        {
                //            client.Blocking = true;
                //            client.Write(globalMenus[globals.ToString().IndexOf(c)]);
                //        }
                //    }

                //}
                ////int pos = client.Data.IndexOfAny(globals);
        }

        private void onDisconnect(object sender, EventArgs e)
        {
            Client client = (Client)sender;



            client.Close();
        }

    }
}




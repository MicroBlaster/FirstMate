using System;
using System.Collections.Generic;
using System.Text;

namespace FirstMate
{
    public class Helper
    {
        public List<Session> Sessions { get; set; }

        public enum Mode { ClientProxy, GameServer }

        public bool ClientProxy { get; private set; }
        public bool GameServer { get; private set; }

        public int Version { get; private set; }
        public int Build { get; private set; }

        public string Banner { get; private set; }

        private Config.Root config = new Config.Root();

        public Helper(Mode mode)
        {
            //TODO: Load version and build from settings
            Version = 1728;
            Build = 107011;

            Banner = String.Format("FirstMate {0} - TWFM Version {1} (Build {2})\r\n" +
                                   "Copyright (C) {3} David McCartney - All Rights Reserved.\r\n",
                                   mode == 0 ? "Client Proxy" : "Game Server", Version, Build, 2017);

            switch (mode)
            {
                case Mode.ClientProxy:
                    ClientProxy = true;
                    GameServer = false;
                    config.Load("ClientProxy.xml");
                    break;

                case Mode.GameServer:
                    ClientProxy = false;
                    GameServer = true;
                    config.Load("GameServer.xml");
                    break;
            }

            Sessions = new List<Session>();



        }

        public void Start()
        {
            foreach (Config.Session cs in config.Sessions)
            {

                Session session = new Session(this);

                foreach (Config.Connection connection in cs.Connections)
                {
                    session.AddServer(connection, Banner);
                }
                Sessions.Add(session);
            }

            foreach (Session session in Sessions)
            {
                session.Start();
            }
        }

        public void Pause()
        {
            foreach (Session session in Sessions)
            {
                session.Pause();
            }
        }

        public void Stop()
        {
            foreach (Session session in Sessions)
            {
                session.Stop();
            }
        }


    }
}

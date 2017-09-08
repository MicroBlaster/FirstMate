using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Terminal;
using FirstMate;

namespace Service
{
    public partial class Service : ServiceBase
    {
        Server server;

        private static List<Session> sessionList = new List<Session>();

        public Service()
        {
            InitializeComponent();


            try
            {
                //server = new Server("localhost");
                //server = new Server("192.168.1.8:2002");
                server = new Server(); // Default binds on any interface with default port

                server.Connected += onConnect;

                server.Banner = "TradeWars(R) FirstMate Proxy - Version 2016.20(Build 1503)\r\n" +
                "Copyright (C) 2016 David McCartney - All Rights Reserved.\r\n";

            }
            catch (Exception)
            {

            }

        }

        protected override void OnStart(string[] args)
        {
            server.Start();
        }

        protected override void OnStop()
        {
            server.Stop();
        }

        private static void onConnect(object sender, EventArgs e)
        {
            sessionList.Add(new Session((Client)sender, sessionList));
        }

    }
}

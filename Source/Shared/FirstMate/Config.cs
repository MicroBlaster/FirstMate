using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Xml.Serialization;

namespace Config
{

    public class Root
    {

        [XmlArrayItem(ElementName = "Session", Type = typeof(Session))]
        public List<Session> Sessions { get; set; }

        [XmlIgnore]
        public string Name { get; private set; }

        public Root()
        {
            Sessions = new List<Session>();
        }

        public void Save(string file)
        {
            //todo:
        }

        public void Load(string file)
        {
            //todo: load from file

            //todo: check if file does not exist:
            Create();
            Save(file);
        }

        private void Create()
        {
            Session session = new Session();

            //TODO: lookup tavern.twfm.net IPAddress from DNS
            IPAddress tavern = new IPAddress(new byte[] { 127, 0, 0, 1 });

            session.Connections.Add(new Connection(ConnectionType.Server, IPAddress.Any, 3000)); // Client listening Port
            session.Connections.Add(new Connection(ConnectionType.Server, IPAddress.Any, 3001)); // Peer listening Port
            session.Connections.Add(new Connection(ConnectionType.Client, tavern, 3002)); // Tavern Chat client
            Sessions.Add(session);
        }

    }

    public class Session
    {
        [XmlArrayItem(ElementName = "Connection", Type = typeof(Connection))]
        public List<Connection> Connections { get; set; }

        public Session()
        {
            Connections = new List<Connection>();
        }
    }

    public enum ConnectionType { Server, Client }

    public class Connection
    {

        [XmlAttribute]
        public ConnectionType Type { get; set; }

        [XmlAttribute]
        public IPAddress IP { get; set; }

        [XmlAttribute]
        public int Port { get; set; }

        public Connection()
        {

        }

        public Connection(ConnectionType type, IPAddress ip, int port)
        {
            Type = type;
            IP = ip;
            Port = port;
        }
    }


    public class ComingSoon
    {
        ResourceManager rm;
        private string id;

        [XmlAttribute]
        public string ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                Key = rm.GetString("MenuItemKey" + value);
                Name = rm.GetString("MenuItemName" + value);
                Description = rm.GetString("MenuItemDescription" + value);
            }
        }

        [XmlArrayItem(ElementName = "MoreSettings", Type = typeof(MoreSetting))]
        public List<MoreSetting> MoreSettings { get; set; }

        [XmlIgnore]
        public string Key { get; private set; }

        [XmlIgnore]
        public string Name { get; private set; }

        [XmlIgnore]
        public string Description { get; private set; }

        public ComingSoon()
        {
            rm = new ResourceManager("FirstMate.Resources.menu", this.GetType().GetTypeInfo().Assembly);
            MoreSettings = new List<MoreSetting>();
        }
    }

    public class MoreSetting
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlText]
        public string Action { get; set; }
    }

    public class Global
    {
        ResourceManager rm;

        [XmlAttribute]
        public char Key { get; set; }

        [XmlAttribute]
        public bool Offline { get; set; }

        [XmlText]
        public char MenuID { get; set; }

        [XmlIgnore]
        public string Prompt { get; private set; }

        public Global()
        {
        }

        public Global(char key, string menuID)
        {
            rm = new ResourceManager("FirstMate.Resources.menu", this.GetType().GetTypeInfo().Assembly);
            Prompt = rm.GetString("GlobalPrompt:" + MenuID);

            Key = key;



        }
    }

}


using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FirstMate
{

    public class Menu
    {

        [XmlAttribute]
        public string ID { get; set; }

        [XmlAttribute]
        public string Locked { get; set; }

        [XmlArrayItem(ElementName = "Item", Type = typeof(MenuItem))]
        public List<MenuItem> Items { get; set; }

        [XmlIgnore]
        public string Name { get; private set; }

        public Menu()
        {
            Items = new List<MenuItem>();
        }
    }

    public class MenuItem
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

        [XmlArrayItem(ElementName = "Action", Type = typeof(MenuAction))]
        public List<MenuAction> Actions { get; set; }

        [XmlIgnore]
        public string Key { get; private set; }

        [XmlIgnore]
        public string Name { get; private set; }

        [XmlIgnore]
        public string Description { get; private set; }

        public MenuItem()
        {
            rm = new ResourceManager("FirstMate.Resources.menu", this.GetType().GetTypeInfo().Assembly);
            Actions = new List<MenuAction>();
        }
    }

    public class MenuAction
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlText]
        public string Action { get; set; }
    }
}
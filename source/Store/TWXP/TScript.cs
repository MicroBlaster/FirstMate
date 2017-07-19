using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace TWXP
{
    public class TScript
    {
        List<string> tscript = new List<string>();


        public TScript()
        {

        }

        public void Load(string fileName)
        {
            if (fileName.ToLower().Contains(".cts"))
            {
                ReadCompiled(fileName);
            }
            else
            {
                ReadTScript(fileName);
            }

        }

        public void Save(string fileName, bool compled)
        {
            if (compled)
            {
                WriteCompiled(fileName);
            }
            else
            {
                WriteTScript(fileName);
            }

        }

        private void ReadTScript(string fileName)
        {
            throw new NotImplementedException();
        }

        private void ReadCompiled(string fileName)
        {
            CTS cts = new CTS();

            if (System.IO.File.Exists(fileName))
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);

                BinaryFormatter formatter = new BinaryFormatter();
                cts = (CTS)formatter.Deserialize(fs);
            }
            else
        	{
                throw new Exception("File not found.");
            }
        }

        private void WriteTScript(string fileName)
        {
            throw new NotImplementedException();
        }

        private void WriteCompiled(string fileName)
        {
            CTS cts = new CTS();

            cts.Header.ProgramName = "TWX Proxy";
            cts.Header.Version = 5;
            cts.Header.DescSize = 10;
            cts.Header.CodeSize = 20;

            //Hashtable addresses = new Hashtable();
            //addresses.Add("Jeff", "123 Main Street, Redmond, WA 98052");
            //addresses.Add("Fred", "987 Pine Road, Phila., PA 19116");
            //addresses.Add("Mary", "PO Box 112233, Palo Alto, CA 94301");


            FileStream fs = new FileStream(fileName, FileMode.Create);

            BinaryFormatter bf = new BinaryFormatter();

            cts.Serialize(fs);
        }

    }

    [Serializable]
    class CTS
    {
        public Header Header { get; set; }

        public CTS()
        {
            Header = new Header();

        }

        public void Serialize(Stream s)
        {
            using(BinaryWriter bw = new BinaryWriter(s))
            {
                bw.Write(Header.ProgramName.ToCharArray());
                bw.Write(Header.Version);
                bw.Write(Header.DescSize);
                bw.Write(Header.CodeSize);
            }

        }
    }

    class Header
    {
        public string ProgramName { get; set; }
        public ushort Version { get; set; }
        public int DescSize { get; set; }
        public int CodeSize { get; set; }

        public Header()
        {

        }
    }
}

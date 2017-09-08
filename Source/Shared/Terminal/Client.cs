/*
  Copyright (C) 2016  David McCartney - All Rights Reserved.
    
  Terminal.Client:
  A communication client inherited from System.Net.Sockets.TcpClient
  
  This file is part of a portable class library which adds application
  protocal support (OSI Layer 7) on top of .Net Core System.Net for use
  in various Client/Server cummunication programs (i.e. Telnet).

  This library free for non-comercial use. Please contact the author
  if you need a commercial license. You can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  any later version.

  You should have received a copy of the GNU General Public License
  along with this library. If not, see <http://www.gnu.org/licenses/>.

  This library is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
  GNU General Public License for more details.
  
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Terminal
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class Client
    {
        private NetworkStream stream;

        private bool active;
        private bool telnetDetected;
        private bool ansiDetected;

        /// <summary>
        /// Occurs when client initialization is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public event EventHandler<EventArgs> Initialized = delegate { };


        /// <summary>
        /// Occurs when data has been received from the client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public event EventHandler<EventArgs> Receive = delegate { };

        /// <summary>
        /// Occurs when the client has disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public event EventHandler<EventArgs> Disconnect = delegate { };

        /// <summary>
        /// TODO: Comment
        /// </summary>
        public IPEndPoint RemoteEP { get; private set; }

        /// <summary>
        /// TODO: Comment
        /// </summary>
        public IPEndPoint LocalEP { get; private set; }

        /// <summary>
        /// TODO: Comment
        /// </summary>
        public string ReverseDNS { get; private set; }

        /// <summary>
        /// TODO: Comment
        /// </summary>
        public string Banner { private get; set; }

        /// <summary>
        /// TODO: Comment
        /// </summary>
        public string Data { get; private set; }

        public Client(TcpClient tcpClient, string banner)
        {
            stream = tcpClient.GetStream();
            stream.ReadTimeout = 500;

            Banner = banner;

            active = false;
            telnetDetected = false;
            ansiDetected = false;

            // Initialize the connection.
            Initialize();
        }

        private async void Initialize()
        {
            byte[] readBuffer = new byte[4096];

            // Get the local and remote endpoints from the stream.
            PropertyInfo socket = stream.GetType().GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
            RemoteEP = (IPEndPoint)((Socket)socket.GetValue(stream, null)).RemoteEndPoint;
            LocalEP = (IPEndPoint)((Socket)socket.GetValue(stream, null)).LocalEndPoint;

            // Start reverse DNS lookup using an asynchronous task.
            Task<IPHostEntry> rdnsTask = Dns.GetHostEntryAsync(IPAddress.Parse(RemoteEP.Address.ToString()));

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //byte[] b = Encoding.GetEncoding("IBM437").GetBytes(text + "\n\r");

            // Send ASCII FF + BS and ANSI Clear Screen 
            Write("\u000c\u0008\u001B[2J\r");

            // Send Telnet handshake
            Telnet.Initialize(stream);

            // Send Banner and Initializing
            Write(Banner + "\r\n\u001B[0;32m  Initializing...");

            // Send ANSI detect.
            byte[] ansi = { 27, 91, 54, 110 }; // ANSI (ESC)[6n - Request Cursor Position
            stream.Write(ansi, 0, ansi.Length);
            await Task.Delay(200);

            try
            {
                // Read the response.
                stream.Read(readBuffer, 0, 1024);

                // Check if response contains an ANSI excape sequence.
                // ansiDetected = Encoding.UTF8.GetString(readBuffer).Contains("\u001B[");
                ansiDetected = Regex.IsMatch(Encoding.UTF8.GetString(readBuffer), "\\x1b[[0-9;]*R");
            }
            catch (Exception)
            {

                // TODO: Log ANSI detection error.
            }


            // TODO: skip ReverseDNS if localhost or local interface
            // Get the reverse DNS result.
            //await Task.Delay(2000);
            ReverseDNS = "UNKNOWN";
            try
            {
                rdnsTask.Wait();
                ReverseDNS = rdnsTask.Result.HostName;
            }
            catch (Exception)
            {
                // TODO: Log reverse DNS error.
            }

            // Send initialized event
            Initialized(this, new EventArgs());
        }

        public async void Start()
        {
            if (active == true) return;
            else active = true;

            await HandleReceiveAsync();

            // Close the connection
            stream.Dispose();

            // Raise disconnected event.
            Disconnect(this, new EventArgs());
        }


        private async Task HandleReceiveAsync()
        {
            byte[] readBuffer = new byte[4096];

            while (active)
            {
                if (await stream.ReadAsync(readBuffer, 0, 1) > 0)
                {
                    Data = "";

                    //todo process telnet
                    //todo process ansi

                    //temporally remove extra characters
                    foreach(byte b in readBuffer)
                    {
                        if(b>31 & b<128)
                        {
                            Data += (char)b;
                        }
                    }

                    // Read data from the stream when avaialbe.
                    // Data = Encoding.UTF8.GetString(readBuffer);

                    // Raise recieved event.
                    Receive(this, new EventArgs());
                }
            }
        }

        public void Write(String text)
        {
            
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            byte[] buffer = Encoding.GetEncoding("IBM437").GetBytes(text);
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Close()
        {
            active = false;
        }
    }
}

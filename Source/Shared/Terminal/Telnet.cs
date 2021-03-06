﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Terminal
{
    public class WindowSize
    {
        public int WindowSizeX { get; set; }
        public int WindowSizeY { get; set; }

        public WindowSize()
        {
            WindowSizeX = -1;
            WindowSizeY = -1;
        }
    }


    public static class Telnet
    {
        private static NetworkStream stream;

        public static void Initialize(NetworkStream Stream)
        {
            stream = Stream;

            // Send Telnet Handshake
            byte[] telnet = {
                    255, 251, 1,    // Telnet (IAC)(Will)(ECHO) - Will Echo
                    255, 251, 3 };  // Telnet (IAC)(Will)(SGA)  - Will Supress Go Ahead
            stream.Write(telnet, 0, telnet.Length);

            Parse();
        }

        /// <summary>
        /// Patrial implimentation of a telnet parser.
        /// Currently supports Window Size and Terminal Type Only.
        /// </summary>
        /// <param name="stream">NetworkStream to parse for telnet sequences.</param>
        public static void Parse()
        {

            try
            {
                byte[] readBuffer = new byte[1024];
                int readBytes = stream.Read(readBuffer, 0, 1024);

                //String response = Encoding.UTF8.GetString(readBuffer);
                //ansiDetected = Encoding.UTF8.GetString(readBuffer).Contains("\u001B[");


                for (int i = 0; i < readBytes; i++)
                {
                    byte[] debug = Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1} ", (char)readBuffer[i], readBuffer[i]));
                    //todo: echo telnet codes if debug is enabled
                    //stream.Write(debug, 0, debug.Length);

                    // Check for IAC
                    if (readBuffer[i] == 255)
                    {
                        switch(readBuffer[i+1])
                        {
                            // Handle WILL
                            case 251:
                                // todo:
                                break;

                            // Handle WONT
                            case 252:
                                // todo:
                                break;

                            // Handle DO
                            case 253:
                                // todo:
                                break;

                            // Handle DONT
                            case 254:
                                // todo:
                                break;
                        }
                    }
                }

                //telnetResponse.WindowSizeX = 80;
                //telnetResponse.WindowSizeY = 24;
                //telnetResponse.TerminlaType = "Testing";
            }
            catch (Exception)
            {
                // TODO: Log Telnet parse error.
            }

            //return telnetResponse;
        }

        public static void GetTerminalType()
        {
            byte[] telnet = {
                    255, 253, 24,   // Telnet (IAC)(DO)(TT)     - Do Terminal Type
                    255, 250, 24, 1, 255, 240, 13, 10 }; //(IAC)(SB)(TT)(1)(IAC)(SE)
            stream.Write(telnet, 0, telnet.Length);

            Parse();
        }

        private static void getWindowSize()
        {
            byte[] telnet = {
                    255, 253, 31 }; // Telnet (IAC)(DO)(WS)    - Do Window Size
            stream.Write(telnet, 0, telnet.Length);

            Parse();
        }


        private static void getWindowX()
        {
            //todo:
        }
        private static void getWindowY()
        {
            //todo:
        }

    }

}


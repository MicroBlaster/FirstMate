using System;
using System.Collections.Generic;
using System.Text;
using Terminal;
using FirstMate;

namespace Daemon
{
    public class Program
    {
        private static List<Session> sessionList = new List<Session>();

        public static void Main(string[] args)
        {
            // Create an instance of the helper calss, which manages all game sessions.
            Helper helper = new Helper(Helper.Mode.ClientProxy); // Defult binds on any interface with default port

            // Display the banner, which contains version and copyright information
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(helper.Banner);

            // Start hte helper.
            Console.Write("Initializing...");
            helper.Start();

            //TODO: Callback log messages
            //Helper.Connected += onConnect;


            Console.WriteLine("\rReady to receive connections. Type 'HELP' for a list of commands.\n");

            while (true)
            {
                Console.Write("Daemon:\\> ");

                switch (Console.ReadLine().ToUpper())
                {
                    case "START":
                        helper.Start();
                        break;

                    case "PAUSE":
                        helper.Pause();
                        break;

                    case "STOP":
                        helper.Stop();
                        break;

                    case "EXIT":
                        helper.Stop();
                        Environment.Exit(0);
                        break;

                    case "HELP":
                        Console.WriteLine("\nAvailable Commands:\n");
                        Console.WriteLine("START  - Start the Helper and listen for incoming connections.");
                        Console.WriteLine("PAUSE  - Existing connections will remain open, but new connections will be rejected.");
                        Console.WriteLine("STOP   - Existing connections will be closed, and the Helper will be shut down.");
                        Console.WriteLine("EXIT   - Close all connections, and Exit the Daemon\n");
                        break;

                    default:
                        Console.WriteLine("*** ERROR *** Command not recognized. Type 'HELP' for a list of commands.\n");
                        break;
                }
            }
        }
    }
}

using System;
using TWXP;

namespace TWXC
{
    class Program
    {
        static void Main(string[] args)
        {
            //System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();

            TScript ts = new TScript();
            ts.Save(@"c:\tw2002\test\test1.cts", true);

            Console.WriteLine("Hello World!");
        }
    }
}

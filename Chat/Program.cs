using System;
using System.Net;

namespace Chat
{
    class Program
    {

        static void Main(string[] args)
        {
            var server = new Server(IPAddress.Loopback);
            server.Start();

            Console.WriteLine("SERVER STARTED: " + DateTime.Now);
            while (Console.ReadKey(true).KeyChar != 'q')
            {
            }

            server.Stop();
        }

    }
}
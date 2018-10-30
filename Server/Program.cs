using System;
using System.Threading;

namespace Server
{
    public class Program
    {
        private static Server _server { get; set; }

        public static void Main(string[] args)
        {
            _server = new Server();

            Console.Title = "(Server) TCP Sample";

            _server.Start();
            _server.Listen();

            Console.WriteLine($"Server is listening on port 7171...");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                ;

            Console.WriteLine("Closing server...");

            _server.Disconnect();
            _server.Stop();

            Thread.Sleep(1000);

            Environment.Exit(0);
        }
    }
}
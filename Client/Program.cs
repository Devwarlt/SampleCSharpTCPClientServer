using System;
using System.Threading;

namespace Client
{
    public class Program
    {
        private static Client _client { get; set; }

        public static void Main(string[] args)
        {
            _client = new Client();

            Console.Title = "(Client) TCP Sample";

            _client.Connect();

            var sender = new Thread(() =>
            {
                do
                {
                    _client.Send((new Random().NextDouble() * 1000).ToString());

                    Thread.Sleep(1000);
                } while (true);
            })
            { IsBackground = true };

            sender.Start();

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                ;

            Console.WriteLine("Closing client...");

            sender.Join();

            _client.Disconnect();

            Thread.Sleep(1000);

            Environment.Exit(0);
        }
    }
}
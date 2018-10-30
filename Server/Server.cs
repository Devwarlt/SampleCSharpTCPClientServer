using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Server
    {
        private TcpListener TcpListener { get; set; }
        private ServerNetwork Network { get; set; }

        public Server()
        {
            TcpListener = TcpListener.Create(7171);
            TcpListener.Server.NoDelay = true;
            TcpListener.Server.UseOnlyOverlappedIO = true;
            TcpListener.Server.Ttl = 112;
            TcpListener.Server.SendTimeout = 1000;
            TcpListener.Server.ReceiveTimeout = 1000;
            Network = new ServerNetwork();
        }

        public void Start() => TcpListener.Start();

        public void Stop() => TcpListener.Stop();

        public async void Listen()
        {
            try
            {
                var client = new Client()
                {
                    Id = Interlocked.Increment(ref Client.LastId),
                    TcpClient = await TcpListener.AcceptTcpClientAsync()
                };

                if (Network.Add(client))
                {
                    Console.WriteLine("New client connected!");

                    client.Handle();
                }
                else
                    Console.WriteLine("Something went wrong with 'ServerNetwork.Add'!");
            }
            catch { }

            Thread.Sleep(100);

            Listen();
        }

        public void Disconnect() => Network.DisconnectAll();
    }

    public class Client
    {
        public static int LastId = 0;

        public int Id { get; set; }
        public TcpClient TcpClient { get; set; }

        private ClientNetwork ClientNetwork { get; set; }

        public Client() => ClientNetwork = new ClientNetwork(this);

        public void Handle() => ClientNetwork.Receive();
    }

    public class ClientNetwork
    {
        private Client _client { get; set; }

        private NetworkStream GetStream => _client.TcpClient.GetStream();

        private bool IsConnected => _client.TcpClient.Connected;

        public ClientNetwork(Client client) => _client = client;

        public async void Send(string data)
        {
            if (!IsConnected)
            {
                Disconnect();

                return;
            }

            var buffer = Encoding.UTF8.GetBytes(data);

            try
            {
                await GetStream.WriteAsync(buffer, 0, buffer.Length);

                Console.WriteLine($"Sent data '{data}' to client {_client.Id}!");
            }
            catch { Console.WriteLine("Something went wrong with 'ClientNetwork.Send'!"); }
        }

        public async void Receive()
        {
            if (!IsConnected)
            {
                Disconnect();

                return;
            }

            var buffer = new byte[4096];

            try
            {
                var len = await GetStream.ReadAsync(buffer, 0, buffer.Length);
                var data = Encoding.UTF8.GetString(buffer, 0, len);

                Console.WriteLine($"Received data '{data}' from client {_client.Id}!");

                Send((new Random().NextDouble() * 1000).ToString());
            }
            catch { Console.WriteLine("Something went wrong with 'ClientNetwork.Receive'!"); }

            Thread.Sleep(100);

            Receive();
        }

        public void Disconnect()
        {
            _client.TcpClient.Close();
            _client.TcpClient.Dispose();

            Console.WriteLine($"Client {_client.Id} has left from the server network!");
        }
    }

    public class ServerNetwork
    {
        private ConcurrentDictionary<int, Client> Clients { get; set; }

        public ServerNetwork()
            => Clients = new ConcurrentDictionary<int, Client>();

        private void ConnectionManager()
        {
            if (Clients.Keys.Count != 0)
            {
                Clients.Values.ToList().Where(client => !client.TcpClient.Connected).Select(client =>
                {
                    client.TcpClient.Client.Close();
                    client.TcpClient.Client.Dispose();

                    Console.WriteLine($"Client {client.Id} has left from the server network!");

                    return client;
                }).ToList();

                Thread.Sleep(1000);

                ConnectionManager();
            }
            else
            {
                Thread.Sleep(5000);

                ConnectionManager();
            }
        }

        public bool Add(Client client)
            => Clients.TryAdd(client.Id, client);

        public void DisconnectAll()
        {
            var amount = Clients.Keys.Count;

            if (amount == 0)
            {
                Console.WriteLine("There is no client in the server network to disconnect!");

                return;
            }

            Console.WriteLine($"Preparing to disconnect {amount} client{(amount > 1 ? "s" : "")} from the server!");

            Clients.Values.ToList().Select(client =>
            {
                Console.WriteLine($"Client {client.Id} has left from the server network!");

                client.TcpClient.Close();
                client.TcpClient.Dispose();

                return client;
            }).ToList();

            Console.WriteLine("All clients have been removed!");
        }
    }
}
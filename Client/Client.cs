using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Client
    {
        public TcpClient TcpClient { get; set; }

        private Network Network { get; set; }

        public Client()
        {
            TcpClient = new TcpClient();
            TcpClient.Client.NoDelay = true;
            TcpClient.Client.UseOnlyOverlappedIO = true;
            TcpClient.Client.Ttl = 112;
            TcpClient.Client.SendTimeout = 1000;
            TcpClient.Client.ReceiveTimeout = 1000;
            Network = new Network(this);
        }

        public void Connect()
        {
            try
            {
                System.Console.WriteLine("Connecting to the server...");

                TcpClient.Connect("127.0.0.1", 7171);

                System.Console.WriteLine("Connected!");

                Network.Receive();
            }
            catch { System.Console.WriteLine("Connection lost! Reconnecting to the server..."); }
        }

        public void Send(string data) => Network.Send(data);

        public void Disconnect() => Network.Disconnect();
    }

    public class Network
    {
        private Client _client { get; set; }

        private NetworkStream GetStream => _client.TcpClient.GetStream();

        private bool IsConnected => _client.TcpClient.Connected;

        private bool Disconnected { get; set; }

        public Network(Client client) => _client = client;

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

                System.Console.WriteLine($"Sent data '{data}' to server!");
            }
            catch { System.Console.WriteLine("Something went wrong with 'Network.Send' data!"); }
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

                System.Console.WriteLine($"Received data '{data}' from server!");
            }
            catch { System.Console.WriteLine("Something went wrong with 'Network.Receive' data!"); }

            Thread.Sleep(100);

            Receive();
        }

        public void Disconnect()
        {
            if (Disconnected)
                return;

            Disconnected = true;

            _client.TcpClient.Close();
            _client.TcpClient.Dispose();

            System.Console.WriteLine("Client has been disconnected from the server!");
        }
    }
}
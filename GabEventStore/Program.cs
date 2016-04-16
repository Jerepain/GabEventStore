using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GabEventStore
{
    class Program
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
        public static bool connected;
        static void Main(string[] args)
        {
            var conn = Init();
            Subscriptions(conn);

            StreamEventsSlice historique = conn.ReadStreamEventsForwardAsync("RoomXMO", 0, 100, true).Result;

            foreach (var resolvedEvent in historique.Events)
            {

                var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data);
                if (string.IsNullOrEmpty(json)) continue; 
                var message = JsonConvert.DeserializeObject<ChatMessage>(json).Message;
                Console.WriteLine(message);
            }

            bool running = true;
            while (running)
            {
                if (connected)
                {
                    string input = Console.ReadLine();
                    conn.AppendToStreamAsync("commetuveux", ExpectedVersion.Any, CreateEventData(input));
                }
                Thread.Sleep(100);
            }



            //Envoyer 
            Console.Read();
        }

        private static IEventStoreConnection Init()
        {
            var connSettings =
                ConnectionSettings.Create()
                    .UseConsoleLogger()
                    .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
            var ip = Dns.GetHostAddresses("ubuntustore.cloudapp.net");
            var conn = EventStore.ClientAPI.EventStoreConnection.Create(ConnectionSettings.Create(),
                new IPEndPoint(IPAddress.Parse(ip.First().ToString()), 1113));
            conn.Connected += ConnOnConnected;
            conn.ConnectAsync();
            return conn;
        }

        private static void Subscriptions(IEventStoreConnection conn)
        {
            conn.SubscribeToStreamAsync("Room0", true, OnEventReceived);
            conn.SubscribeToStreamAsync("RoomXMO", true, OnEventReceived);
            conn.SubscribeToStreamAsync("Room2", true, OnEventReceived);
        }

        private static void OnEventReceived(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
        {
            var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data);
            var message = JsonConvert.DeserializeObject<ChatMessage>(json).Message;
            Console.WriteLine(message);
        }

        private static EventData[] CreateEventData(string input)
        {
            return new EventData[]
            {
                new EventData(Guid.NewGuid(), "ChatMessage", true, CreateMessage(input), new byte[0])
            };
        }

        private static void ConnOnConnected(object sender, ClientConnectionEventArgs e)
        {
            Console.WriteLine("connected");
            connected = true;
        }

        private static byte[] CreateMessage(string input)
        {
            var message = new ChatMessage
            {
                User = "Jerem",
                Message = input
            };
            var json = JsonConvert.SerializeObject(message, SerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Chat;

namespace Test
{
    class Test
    {

        private static Server _server;

        private static List<string> _people;

        private static List<Tuple<string, string>> _testMessages;

        static void Main(string[] args)
        {
            var numberOfTests = 10;
            for (var i = 0; i < numberOfTests; i++)
            {
                Console.WriteLine($"STARTING TEST #{i}...");
                
                StartServer();
                GenerateRandomTest();

                var clients = new Dictionary<string, TestClient>();
                foreach (var user in _people)
                {
                    var client = new TestClient();
                    client.DiscardEcho();
                    Receive(client);
                    SendClientMessage(client, user, true);
                    clients[user] = client;
                }

                // start chat
                foreach (var testMessage in _testMessages)
                {
                    var user = testMessage.Item1;
                    var message = testMessage.Item2;
                    var client = clients[user];
                    SendClientMessage(client, message);
                    VerifyTestResult(user, message,
                                     clients.Where(kvp => kvp.Key != user).Select(kvp => kvp.Value).ToList());
                }

                // close clients
                foreach (var client in clients.Values)
                {
                    client.Close();
                }

                StopServer();

                Console.WriteLine($"TEST #{i} PASSED.");
            }

            Console.WriteLine($"CHAT PASSED ALL {numberOfTests} TESTS!");
        }

        private static void SendClientMessage(TestClient client,
                                              string message,
                                              bool echo = false)
        {
            var response = client.SendMessage(message);
            if (echo) Console.Write(response);

            client.DiscardEcho();

            response = client.SendMessage("\r\n");
            if (echo) Console.Write(response);

            response = client.ReceiveMessage();
            Console.Write(response);
        }

        private static string Receive(TestClient client)
        {
            var response = client.ReceiveMessage();
            Console.WriteLine(response);
            return response;
        }

        private static void StartServer()
        {
            _server = new Server(IPAddress.Loopback, false);
            _server.Start();
            Console.WriteLine("SERVER STARTED: " + DateTime.Now);
        }

        private static void StopServer()
        {
            _server.Stop();
        }

        private static void GenerateRandomTest()
        {
            _people = new List<string>();
            _testMessages = new List<Tuple<string, string>>();
            var names = new[] {"Franco", "Ciccio", "Bud", "Terence", "Goku"};
            var numberOfParticipants = new Random().Next(2, 5);
            var numberOfMessages = new Random().Next(10, 50);
            _people = names.Take(numberOfParticipants).ToList();
            for (var i = 0; i < numberOfMessages; i++)
            {
                var user = _people[i % numberOfParticipants];
                var message = $"Message {i}";
                _testMessages.Add(new Tuple<string, string>(user, message));
            }
        }

        private static void VerifyTestResult(string user,
                                             string message,
                                             List<TestClient> otherClients)
        {
            if (otherClients.Count != _people.Count - 1) throw new Exception("TEST FAILED");
            foreach (var client in otherClients)
            {
                var receivedMessage = Receive(client);
                if (receivedMessage != $"{user}: {message}")
                    throw new Exception($"TEST FAILED: received '{receivedMessage}', expecting '{user}: {message}'");
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace TcpNode
{
	public static class Program
	{
		private static readonly List<Client> _clients = new List<Client>();
		static void Main(string[] args)
		{
			Console.WriteLine("TCP connector starting");

			Connector.RegisterListener("tcp.connect", tcp_connect);
			Connector.RegisterListener("tcp.send", tcp_send);

			Connector.OnNodeListenerDiscovered("tcp.init", sender => sender.Reply(new Message("tcp.init")));

			Connector.Run();
		}

		private static void tcp_send(Node sender, Message message)
		{
			string host = message.Body["host"] as string;
			long? port = message.Body["port"] as long?;
			string textToSend = message.Body["message"] as string;

			if (string.IsNullOrEmpty(host) || !port.HasValue || string.IsNullOrEmpty(textToSend)) return;

			Client client = _clients.FirstOrDefault(c => c.Host == host && c.Port == port);
			if (client == null) return;
			
			byte[] bytes = Convert.FromBase64String(textToSend);
			Console.WriteLine("Sending {0}", Encoding.UTF8.GetString(bytes));
			client.TcpClient.GetStream().Write(bytes, 0, bytes.Length);
		}

		private static void tcp_connect(Node sender, Message message)
		{
			string host = message.Body["host"] as string;
			long? port = message.Body["port"] as long?;

			if (string.IsNullOrEmpty(host) || !port.HasValue) return;
			bool wasConnected = false;

			Client client = _clients.FirstOrDefault(c => c.Host == host && c.Port == port.Value);
			if (client == null)
			{
				try
				{
					client = new Client(host, (int) port.Value);
					_clients.Add(client);
				}
				catch
				{
					Debug.Assert(client != null);
				}
			}
			else
			{
				wasConnected = client.Connected;
			}
			sender.Reply(new Message("tcp.status", new Dictionary<string, object>
			{
				{ "host", host },
				{ "port", port.Value },
				{ "connected", client.Connected },
				{ "was_connected", wasConnected }
			}));
		}
	}
}

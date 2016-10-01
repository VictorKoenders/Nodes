using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace IRCBotNode
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.Sleep(1000);
			Console.WriteLine("IRC bot starting");
			Connector.RegisterListener("tcp.init", TcpInit);
			Connector.RegisterListener("tcp.status", TcpStatusChanged);
			Connector.RegisterListener("tcp.data", TcpDataReceived);

			Connector.OnNodeListenerDiscovered("tcp.connect", sender => sender.Reply(new Message("tcp.connect", new Dictionary<string, object>
			{
				{"host", "irc.esper.net"},
				{"port", 6667}
			})));
			
			Connector.Run();
		}

		private static void TcpInit(Node sender, Message message)
		{
			sender.Reply(new Message("tcp.connect", new Dictionary<string, object>
			{
				{"host", "irc.esper.net"},
				{"port", 6667}
			}));
		}

		private static readonly List<TcpBuffer> buffer = new List<TcpBuffer>();

		private static void TcpDataReceived(Node sender, Message message)
		{
			string host = message.Body["host"] as string;
			long? port = message.Body["port"] as long?;
			string data = message.Body["data"] as string;

			if (string.IsNullOrEmpty(host) || !port.HasValue || string.IsNullOrEmpty(data)) return;

			byte[] dataBytes = Convert.FromBase64String(data);

			TcpBuffer bufferItem = buffer.FirstOrDefault(b => b.Host == host && b.Port == port.Value);
			if (bufferItem == null)
			{
				bufferItem = new TcpBuffer {Host = host, Port = (int)port.Value, Buffer = new List<byte>()};
				buffer.Add(bufferItem);
			}

			bufferItem.Buffer.AddRange(dataBytes);

			int index = bufferItem.Buffer.IndexOf((byte) '\r');

			while (index >= 0)
			{
				if (index + 2 >= bufferItem.Buffer.Count) break;
				byte[] sub = bufferItem.Buffer.Take(index).ToArray();
				bufferItem.Buffer.RemoveRange(0, index + 2);

				index = bufferItem.Buffer.IndexOf((byte)'\r');

				string line = Encoding.UTF8.GetString(sub);

				Console.WriteLine(line);

				if (line.StartsWith("PING :"))
				{
					sender.Reply(new Message("tcp.send", new Dictionary<string, object>
					{
						{ "host", host },
						{ "port", port },
						{ "message", Convert.ToBase64String(Encoding.UTF8.GetBytes("PONG " + line.Substring(5) + "\r\n")) }
					}));
				}
			}
		}

		private static void TcpStatusChanged(Node sender, Message message)
		{
			bool? wasConnected = message.Body["was_connected"] as bool?;
			bool? isConnected = message.Body["connected"] as bool?;

			if (!wasConnected.HasValue || !isConnected.HasValue) return;

			if (!wasConnected.Value && isConnected.Value)
			{
				byte[] dataToSend = Encoding.UTF8.GetBytes("NICK TrangarBot2\r\nUSER TrangarBot2 TrangarBot2 irc.esper.net :TrangarBot2\r\n");
				sender.Reply(new Message("tcp.send", new Dictionary<string, object>
				{
					{ "host", message.Body["host"] },
					{ "port", message.Body["port"] },
					{ "message", Convert.ToBase64String(dataToSend) },
				}));
			}
		}
	}
}

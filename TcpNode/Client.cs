using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Shared;

namespace TcpNode
{
	public class Client
	{
		public Client(string host, int port)
		{
			Host = host;
			Port = port;

			TcpClient = new TcpClient(host, port);

			TcpClient.GetStream().BeginRead(buffer, 0, 1024, DataRead, null);
		}

		private async void ConnectAfterDelay()
		{
			await Task.Delay(5000);
			try
			{
				TcpClient.Connect(Host, Port);

				Connector.Emit(new Message("tcp.status", new Dictionary<string, object>
				{
					{ "host", Host },
					{ "port", Port },
					{ "connected", true },
					{ "was_connected", false }
				}));
			}
			catch
			{
				ConnectAfterDelay();
			}
		}

		private void DataRead(IAsyncResult ar)
		{
			int length = TcpClient.GetStream().EndRead(ar);

			if (length == 0)
			{
				Connector.Emit(new Message("tcp.status", new Dictionary<string, object>
				{
					{ "host", Host },
					{ "port", Port },
					{ "connected", false },
					{ "was_connected", true }
				}));

				ConnectAfterDelay();
				return;
			}

			Connector.Emit(new Message("tcp.data", new Dictionary<string, object>
			{
				{ "host", Host },
				{ "port", Port },
				{ "data", Convert.ToBase64String(buffer, 0, length) }
			}));
			
			TcpClient.GetStream().BeginRead(buffer, 0, 1024, DataRead, null);
		}

		public string Host { get; set; }
		public int Port { get; set; }

		private readonly byte[] buffer = new byte[1024];
		public TcpClient TcpClient { get; set; }
		public bool Connected => TcpClient.Connected;
	}
}
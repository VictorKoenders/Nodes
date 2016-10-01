using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace Shared
{
	internal class NodeConnector
	{
		public delegate void MessageReceivedEvent(string host, int port, Message message);
		public delegate void NodeDiscoveredEvent(string host, int port);
		public delegate void NodeRemovedEvent(string host, int port);

		public MessageReceivedEvent MessageReceived;
		public NodeDiscoveredEvent NodeDiscovered;
		public NodeRemovedEvent NodeRemoved;
		private UdpClient client;
		
		public async void SendTo(string host, int port, Message message)
		{
			string headerText = JsonConvert.SerializeObject(message.Header);
			string bodyText = JsonConvert.SerializeObject(message.Body);

			List<byte> bytesToSend = new List<byte>();
			byte[] nameBytes = Encoding.UTF8.GetBytes(message.Name);
			byte[] headerBytes = Encoding.UTF8.GetBytes(headerText);
			byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyText);

			byte[] nameLengthBytes = BitConverter.GetBytes((ushort) nameBytes.Length);
			byte[] headerLengthBytes = BitConverter.GetBytes((ushort)headerBytes.Length);
			byte[] bodyLengthBytes = BitConverter.GetBytes((ushort)bodyBytes.Length);

			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(nameLengthBytes);
				Array.Reverse(headerLengthBytes);
				Array.Reverse(bodyLengthBytes);
			}

			bytesToSend.AddRange(nameLengthBytes);
			bytesToSend.AddRange(headerLengthBytes);
			bytesToSend.AddRange(bodyLengthBytes);
			bytesToSend.AddRange(nameBytes);
			bytesToSend.AddRange(headerBytes);
			bytesToSend.AddRange(bodyBytes);

			byte[] lengthBytes = BitConverter.GetBytes((ushort) (bytesToSend.Count + 2));
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(lengthBytes);
			}

			bytesToSend.InsertRange(0, lengthBytes);

			byte[] bytesToSendArray = bytesToSend.ToArray();
			
			await client.SendAsync(bytesToSendArray, bytesToSendArray.Length, new IPEndPoint(IPAddress.Parse(host), port));
		}

		private readonly List<ConnectorBuffer> buffer = new List<ConnectorBuffer>();

		private void ReceiveCallback(IAsyncResult ar)
		{
			IPEndPoint ipEndPoint = null;
			byte[] bytes = client.EndReceive(ar, ref ipEndPoint);

			ConnectorBuffer bufferItem = buffer.FirstOrDefault(b => b.Host == ipEndPoint.Address.ToString() && b.Port == ipEndPoint.Port);
			if (bufferItem == null)
			{
				bufferItem = new ConnectorBuffer();
				bufferItem.Host = ipEndPoint.Address.ToString();
				bufferItem.Port = ipEndPoint.Port;
				bufferItem.Buffer = new List<byte>();
				buffer.Add(bufferItem);

				NodeDiscovered?.Invoke(bufferItem.Host, bufferItem.Port);
			}

			bufferItem.Buffer.AddRange(bytes);

			foreach (Message message in bufferItem.GetMessages())
			{
				MessageReceived?.Invoke(bufferItem.Host, bufferItem.Port, message);
			}
			client.BeginReceive(ReceiveCallback, null);
		}

		private void HostOn(int port)
		{
			client = new UdpClient(port);
			Console.WriteLine("Hosting on {0}:{1}", ((IPEndPoint)client.Client.LocalEndPoint).Address, ((IPEndPoint)client.Client.LocalEndPoint).Port);
		}

		public void Connect()
		{
			try
			{
				HostOn(50505);
			}
			catch (Exception ex)
			{
				HostOn(0);
				NodeDiscovered?.Invoke("127.0.0.1", 50505);
			}

			client.BeginReceive(ReceiveCallback, null);
		}
	}
}
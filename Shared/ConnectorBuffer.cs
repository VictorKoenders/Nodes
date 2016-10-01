using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Shared
{
	internal class ConnectorBuffer
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public List<byte> Buffer { get; set; }

		public IEnumerable<Message> GetMessages()
		{
			while (true)
			{
				if (Buffer.Count < 8) yield break;
				// first bytes are:
				// - 2 bytes: total message length
				// - 2 bytes: name length
				// - 2 bytes: header length
				// - 2 bytes: body length

				byte[] messageLengthBytes = Buffer.Take(2).ToArray();
				byte[] nameLengthBytes = Buffer.Skip(2).Take(2).ToArray();
				byte[] headerLengthBytes = Buffer.Skip(4).Take(2).ToArray();
				byte[] bodyLengthBytes = Buffer.Skip(6).Take(2).ToArray();

				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(messageLengthBytes);
					Array.Reverse(nameLengthBytes);
					Array.Reverse(headerLengthBytes);
					Array.Reverse(bodyLengthBytes);
				}

				int messageLength = BitConverter.ToUInt16(messageLengthBytes, 0);
				int nameLength = BitConverter.ToUInt16(nameLengthBytes, 0);
				int headerLength = BitConverter.ToUInt16(headerLengthBytes, 0);
				int bodyLength = BitConverter.ToUInt16(bodyLengthBytes, 0);

				if (messageLength < Buffer.Count) yield break;

				byte[] messageBytes = Buffer.Skip(8).Take(messageLength - 8).ToArray();
				Buffer.RemoveRange(0, messageLength);

				if (nameLength + headerLength + bodyLength + 8 != messageLength)
				{
					// lengths don't add up
					yield break;
				}

				string name = Encoding.UTF8.GetString(messageBytes, 0, nameLength);
				string headerString = Encoding.UTF8.GetString(messageBytes, nameLength, headerLength);
				string bodyString = Encoding.UTF8.GetString(messageBytes, nameLength + headerLength, bodyLength);

				yield return new Message(name, JsonConvert.DeserializeObject<Dictionary<string, object>>(headerString), JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyString));
			}

			yield break;
		}
	}
}
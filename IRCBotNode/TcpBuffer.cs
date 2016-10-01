using System.Collections.Generic;

namespace IRCBotNode
{
	internal class TcpBuffer
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public List<byte> Buffer { get; set; }
	}
}
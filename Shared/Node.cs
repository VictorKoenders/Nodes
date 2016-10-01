using System.Collections.Generic;
using System.Linq;

namespace Shared
{
	public class Node
	{
		internal string Host { get; set; }
		internal int Port { get; set; }
		internal Message Message { get; set; }
		internal List<string> Listeners { get; set; } = new List<string>();

		public void Reply(Message message)
		{
			Connector.connector.SendTo(Host, Port, message);
		}

		public bool ListensTo(string message)
		{
			return Listeners.Any(l => Message.CheckAppliesTo(message, l));
		}
	}
}
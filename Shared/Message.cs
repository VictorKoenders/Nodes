using System.Collections.Generic;

namespace Shared
{
	public class Message
	{
		public Message(string name)
		{
			Name = name;
			Header = new Dictionary<string, object>();
			Body = new Dictionary<string, object>();
		}

		public Message(string name, Dictionary<string, object> body)
		{
			Name = name;
			Header = new Dictionary<string, object>();
			Body = body;
		}

		public Message(string name, Dictionary<string, object> header, Dictionary<string, object> body)
		{
			Name = name;
			Header = header;
			Body = body;
		}

		public string Name { get; set; }
		public Dictionary<string, object> Header{ get; set; }
		public Dictionary<string, object> Body { get; set; }

		internal static bool CheckAppliesTo(string name, string listenerName)
		{
			string[] nameParts = name.Split('.');
			string[] listenerParts = listenerName.Split('.');

			for (int i = 0; i < listenerParts.Length; i++)
			{
				if (i > nameParts.Length) return false;
				if (listenerParts[i] == "*") return true;
				if (listenerParts[i] != nameParts[i]) return false;
			}
			return true;
		}

		public bool AppliesTo(string listenerName)
		{
			return CheckAppliesTo(Name, listenerName);
		}
	}
}
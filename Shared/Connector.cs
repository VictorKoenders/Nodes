using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Shared
{
    public static class Connector
	{
		public delegate void ListenerCallback(Node sender, Message message);
		public delegate void NodeDiscoveredCallback(Node sender);

		private static long MessageID;
	    private static readonly Dictionary<long, TaskCompletionSource<Message>> replyMessages = new Dictionary<long, TaskCompletionSource<Message>>();
		internal static readonly NodeConnector connector = new NodeConnector();
		private static List<Tuple<string, NodeDiscoveredCallback>> nodeDiscoveredCallbacks = new List<Tuple<string, NodeDiscoveredCallback>>();
		private static readonly Dictionary<string, ListenerCallback> listeners = new Dictionary<string, ListenerCallback>();
		//private static readonly Dictionary<string, List<Node>> remoteListeners = new Dictionary<string, List<Node>>();
		private static readonly List<Node> connectedNodes = new List<Node>();
		
	    static Connector()
	    {
		    connector.MessageReceived += MessageReceived;
			connector.NodeDiscovered += NodeDiscovered;
			connector.NodeRemoved += NodeRemoved;

		    connector.Connect();
	    }

	    private static void MessageReceived(string host, int port, Message message)
	    {
		    if (message.Header.ContainsKey("reply-to"))
		    {
				long? id = message.Header["reply-to"] as long?;
			    TaskCompletionSource<Message> task;
			    if (id.HasValue && replyMessages.TryGetValue(id.Value, out task))
			    {
				    task.SetResult(message);
				    replyMessages.Remove(id.Value);
				    return;
			    }
		    }
		    Node node = EnsureNode(host, port);
		    if (message.AppliesTo("system.*"))
		    {
			    if (message.AppliesTo("system.register_events"))
				{
					node.Message = message;
					AddEventsToNode(node, (message.Body["events"] as JArray)?.ToObject<List<string>>());
				}
		    }
		    else
		    {
			    foreach (KeyValuePair<string, ListenerCallback> listener in listeners)
			    {
				    if (message.AppliesTo(listener.Key))
				    {
					    node.Message = message;
					    listener.Value.Invoke(node, message);
				    }
			    }
		    }
	    }

	    private static Node EnsureNode(string host, int port)
	    {
		    Node node = connectedNodes.FirstOrDefault(n => n.Host == host && n.Port == port);
		    if (node == null)
		    {
			    node = new Node {Host = host, Port = port};
			    connectedNodes.Add(node);
		    }
		    return node;
	    }

	    private static void AddEventsToNode(Node sender, List<string> list)
	    {
		    if (list != null)
		    {
			    sender.Listeners.AddRange(list);

			    foreach (string item in list)
			    {
				    foreach (Tuple<string, NodeDiscoveredCallback> nodeDiscoveredListener in nodeDiscoveredCallbacks)
				    {
					    if (Message.CheckAppliesTo(nodeDiscoveredListener.Item1, item))
					    {
						    nodeDiscoveredListener.Item2.Invoke(sender);
					    }
				    }
			    }
		    }
	    }

	    private static void NodeDiscovered(string host, int port)
	    {
		    if (!connectedNodes.All(n => n.Host != host || n.Port != port)) return;

		    Console.WriteLine("Found node {0}:{1}", host, port);
		    Node node = new Node {Host = host, Port = port};
		    connectedNodes.Add(node);
		    node.Reply(new Message("system.register_events", new Dictionary<string, object>
		    {
			    { "events", listeners.Select(l => l.Key).ToList() }
		    }));
	    }

	    public static void OnNodeListenerDiscovered(string listener, NodeDiscoveredCallback cb)
	    {
		    nodeDiscoveredCallbacks.Add(new Tuple<string, NodeDiscoveredCallback>(listener, cb));
	    }

		private static void NodeRemoved(string host, int port)
		{
			connectedNodes.RemoveAll(n => n.Host == host && n.Port == port);
		}

		public static void Emit(Message message)
		{
			foreach (Node node in connectedNodes)
			{
				foreach (string listener in node.Listeners)
				{
					if (message.AppliesTo(listener))
					{
						connector.SendTo(node.Host, node.Port, message);
					}
				}
			}

	    }
	    public static async Task<Message> EmitWithCallback(Message message)
	    {
			TaskCompletionSource<Message> result = new TaskCompletionSource<Message>();

			long id = ++MessageID;

		    if (MessageID >= 1000000) MessageID = 1;
			message.Header.Add("reply-id", id);
		    Emit(message);

			replyMessages.Add(id, result);
			
			return await result.Task;
	    }

	    public static void RegisterListener(string name, ListenerCallback callback)
	    {
			listeners.Add(name, callback);

		    foreach (Node node in connectedNodes)
		    {
				node.Reply(new Message("system.register_events", new Dictionary<string, object>
				{
					{ "events", new List<string> { name }}
				}));
			}
		}

	    public static void Run()
	    {
			while (true)
			{
				Thread.Sleep(1000);
			}
	    }
    }
}

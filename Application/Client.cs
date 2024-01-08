using System.Net.WebSockets;
using CDPStudio.Events;
using CDPStudio.Handlers;
using CDPStudio.Models;

namespace CDPStudio;

class Client
{
    private Uri uri;
    private ClientWebSocket socket;
    private PacketReceiver receiver;
    private PacketSender sender;
    private EventChannel channel;
    private ValueSubscriber value_subscriber;
    private TreeNode? root;

    public Client(string endpoint)
    {
        uri = new Uri(endpoint);
        socket = new ClientWebSocket();

        channel = new EventChannel();
        sender = new PacketSender(socket, channel);
        receiver = new PacketReceiver(socket, channel);
        value_subscriber = new ValueSubscriber(sender, channel);
    }

    public Task ConnectAsync() {
        return Initialize();
    }

    public void Connect() {
        Initialize().Wait();
    }

    private async Task Initialize()
    {
        await socket.ConnectAsync(uri, CancellationToken.None);
        await Task.Run(() => receiver.Listen());

        await receiver.connected.Task;

        Node node = await sender.MakeStructureRequest(0);

        root = new TreeNode("", node, value_subscriber, sender);
    }

    public Hello? Metadata()
    {
        return receiver.metadata;
    }

    public Task<TreeNode> Find(string route)
    {
        TreeNode local = SearchLocalTree(route);
        return SearchStudioTree(route, local);
    }

    private TreeNode SearchLocalTree(string route)
    {
        string[] lineage = route.Split(".");

        TreeNode last = root!;

        for (int i = 1; i < lineage.Length; i++)
        {
            if (!last.HasChild(lineage[i]))
            {
                break;
            }

            last = last.GetChild(lineage[i])!;
        }

        return last;
    }

    private async Task<TreeNode> SearchStudioTree(string route, TreeNode last)
    {
        string[] lineage = route.Split(".");
        int start = Array.IndexOf(lineage, last.name);

        if (route == last.route) {
            return last;
        }

        TreeNode temp = last;

        for (int i = start; i < lineage.Length; i++)
        {
            Node node = await sender.MakeStructureRequest(temp.id);

            foreach (Node child in node.Node_)
            {
                temp.InsertChild(node);
            }

            if (!temp.HasChild(lineage[i]))
            {
                throw new Exception($"Invalid route {route}");
            }

            temp = temp.GetChild(lineage[i])!;
        }

        return temp;
    }
}
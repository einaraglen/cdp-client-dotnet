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
    private StructureEmitter struct_channel;
    private ValueEmitter value_channel;
    private ValueSubscriber value_subscriber;
    private TreeNode? root;

    public Client(string endpoint)
    {
        uri = new Uri(endpoint);
        socket = new ClientWebSocket();

        struct_channel = new StructureEmitter();
        value_channel = new ValueEmitter();

        sender = new PacketSender(socket, struct_channel);
        receiver = new PacketReceiver(socket, struct_channel, value_channel);

        value_subscriber = new ValueSubscriber(sender, value_channel);
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

        root = new TreeNode("", node, value_subscriber);
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
        string[] heritage = route.Split(".");

        TreeNode last = root!;

        for (int i = 1; i < heritage.Length; i++)
        {
            if (!last.HasChild(heritage[i]))
            {
                break;
            }

            last = last.GetChild(heritage[i])!;
        }

        return last;
    }

    private async Task<TreeNode> SearchStudioTree(string route, TreeNode last)
    {
        string[] heritage = route.Split(".");
        int start = Array.IndexOf(heritage, last.name);

        if (route == last.route) {
            return last;
        }

        TreeNode temp = last;

        for (int i = start; i < heritage.Length; i++)
        {
            Node node = await sender.MakeStructureRequest(temp.id);

            foreach (Node child in node.Node_)
            {
                temp.InsertChild(node);
            }

            if (!temp.HasChild(heritage[i]))
            {
                throw new Exception($"Invalid route {route}");
            }

            temp = temp.GetChild(heritage[i])!;
        }

        return temp;
    }
}
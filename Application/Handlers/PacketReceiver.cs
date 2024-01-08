using System.Net.WebSockets;
using CDPStudio.Events;

namespace CDPStudio.Handlers;

class PacketReceiver
{
    private ClientWebSocket socket;
    public Hello? metadata { get; set; }
    public TaskCompletionSource<bool> connected { get; }
    private EventChannel channel;
    public PacketReceiver(ClientWebSocket socket, EventChannel channel)
    {
        this.socket = socket;
        this.channel = channel;
        connected = new TaskCompletionSource<bool>();
    }

    public void OnPacketReceived(MemoryStream stream)
    {
        if (metadata == null)
        {
            ParseHelloMessage(stream);
            return;
        }

        Container packet = Container.Parser.ParseFrom(stream);
        switch (packet.MessageType)
        {
            case Container.Types.Type.EStructureResponse:
                ParseStructureResponse(packet.StructureResponse.ToList());
                break;
            case Container.Types.Type.EGetterResponse:
                ParseGetterResponse(packet.GetterResponse.ToList());
                break;
            case Container.Types.Type.ERemoteError:
                ParseErrorResponse(packet.Error);
                break;
            default:
                break;
        }
    }

    private void ParseStructureResponse(List<Node> response)
    {
        foreach (Node node in response)
        {
            channel.RaiseStructure(node);
        }
    }

    private void ParseGetterResponse(List<VariantValue> response)
    {
        foreach (VariantValue value in response)
        {
            channel.RaiseValue(value);
        }
    }

    private void ParseErrorResponse(Error response)
    {
        throw new Exception(response.Text);
    }

    private void ParseHelloMessage(MemoryStream stream)
    {
        metadata = Hello.Parser.ParseFrom(stream);

        if (!metadata.Challenge.IsEmpty)
        {
            throw new NotSupportedException("Missing Credentials");
        }

        connected.SetResult(true);
    }

    public async Task Listen()
    {
        while (socket.State == WebSocketState.Open)
        {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            using (MemoryStream stream = new MemoryStream(buffer, 0, result.Count))
            {
                OnPacketReceived(stream);
            }
        }
    }
}
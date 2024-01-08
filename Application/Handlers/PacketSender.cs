using System.Net.WebSockets;
using CDPStudio.Events;
using Google.Protobuf;

namespace CDPStudio.Handlers;

class PacketSender
{
    private ClientWebSocket socket;
    private EventChannel channel;
    
    public PacketSender(ClientWebSocket socket, EventChannel channel)
    {
        this.socket = socket;
        this.channel = channel;
    }

    public async Task<Node> MakeStructureRequest(uint id)
    {
        Container packet = new Container { MessageType = Container.Types.Type.EStructureRequest };
        packet.StructureRequest.Add(id);

        TaskCompletionSource<Node> result = new TaskCompletionSource<Node>();

        EventHandler<Node> callback = (object? sender, Node node) => {
            if (node.Info.NodeId == id) {
                result.SetResult(node);
            }
        };

        await SendPacket(packet);

        channel.OnStructure += callback;
        Node node = await result.Task;
        channel.OnStructure -= callback;

        return node;
    }

    public async Task MakeGetterRequest(uint id, double fs, double sampleRate, bool stop)
    {
        ValueRequest request = new ValueRequest { NodeId = id, Fs = fs, SampleRate = sampleRate, Stop = stop };
        Container packet = new Container { MessageType = Container.Types.Type.EGetterRequest };
        packet.GetterRequest.Add(request);
        await SendPacket(packet);
    }

    public async Task MakeSetRequest(uint id, CDPValueType type, object value, ulong time)
    {
        VariantValue request = new VariantValue { NodeId = id, Timestamp = time };

        switch (type)
        {
            case CDPValueType.EDouble:
                request.DValue = (double)value;
                break;
            case CDPValueType.EFloat:
                request.FValue = (float)value;
                break;
            case CDPValueType.EUint64:
                request.Ui64Value = (ulong)value;
                break;
            case CDPValueType.EInt64:
                request.I64Value = (long)value;
                break;
            case CDPValueType.EUint:
                request.UiValue = (uint)value;
                break;
            case CDPValueType.EInt:
                request.IValue = (int)value;
                break;
            case CDPValueType.EUshort:
                request.UsValue = (uint)value;
                break;
            case CDPValueType.EShort:
                request.SValue = (int)value;
                break;
            case CDPValueType.EUchar:
                request.UcValue = (uint)value;
                break;
            case CDPValueType.EChar:
                request.CValue = (int)value;
                break;
            case CDPValueType.EBool:
                request.BValue = (bool)value;
                break;
            case CDPValueType.EString:
                request.StrValue = (string)value;
                break;
            default:
                throw new ArgumentException($"Cannot set value for type {type}");
        }

        Container packet = new Container { MessageType = Container.Types.Type.ESetterRequest };
        packet.SetterRequest.Add(request);
        await SendPacket(packet);
    }

    private async Task SendPacket(Container packet) {
        byte[] messageBytes = packet.ToByteArray();
        await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}   
using CDPStudio.Handlers;

namespace CDPStudio.Events;

class ValueSubscriber
{
    private PacketSender sender;
    private Dictionary<uint, HashSet<Action<VariantValue>>> subscribers;
    private const int FS = 100;
    private const int SAMPLE_RATE = 0;
    public ValueSubscriber(PacketSender sender, ValueEmitter value_channel)
    {
        subscribers = new Dictionary<uint, HashSet<Action<VariantValue>>>();
        this.sender = sender;
        value_channel.OnEvent += PropegateEvent;
    }

    public void RegisterSubscriber(uint id, Action<VariantValue> callback)
    {
        if (subscribers.ContainsKey(id))
        {
            if (subscribers[id].Count == 0)
            {
                ManageValueStream(id, CDPCommand.START);
            }

            subscribers[id].Add(callback);
        }
        else
        {
            subscribers[id] = new HashSet<Action<VariantValue>> { callback };            
            ManageValueStream(id, CDPCommand.START);
        }
    }

    public void UnregisterSubscriber(uint id, Action<VariantValue> callback)
    {
        if (subscribers.ContainsKey(id))
        {
            subscribers[id].Remove(callback);

            if (subscribers[id].Count == 0)
            {
                ManageValueStream(id, CDPCommand.STOP);
            }
        }
    }

    private void ManageValueStream(uint id, CDPCommand cmd)
    {
        Task.Run(() => sender.MakeGetterRequest(id, FS, SAMPLE_RATE, cmd == 0 ? false : true));
    }

    private void PropegateEvent(object? sender, VariantValue value)
    {
        if (subscribers.ContainsKey(value.NodeId))
        {
            foreach (Action<VariantValue> callback in subscribers[value.NodeId])
            {
                callback.Invoke(value);
            }
        }
    }

    private enum CDPCommand {
        START,
        STOP
    }
}
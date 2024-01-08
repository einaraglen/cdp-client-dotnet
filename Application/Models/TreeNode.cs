using CDPStudio.Events;

namespace CDPStudio.Models;

class TreeNode
{
    public uint id { get; set; }
    public string name { get; set; }
    public CDPNodeType nodeType { get; set; }
    public CDPValueType valueType { get; set; }
    public string route { get; set; }
    private Dictionary<string, TreeNode> children { get; set; }
    private ValueSubscriber subscriber;

    public TreeNode(string parent, Node node, ValueSubscriber subscriber)
    {
        id = node.Info.NodeId;
        name = node.Info.Name;
        nodeType = node.Info.NodeType;
        valueType = node.Info.ValueType;
        route = parent + "." + node.Info.Name;

        this.subscriber = subscriber;

        children = new Dictionary<string, TreeNode>();

        if (node.Node_.Count != 0)
        {
            foreach (Node child in node.Node_)
            {
                children.Add(child.Info.Name, new TreeNode(route, child, subscriber));
            }
        }
    }

    public bool HasChild(string name) {
        return children.ContainsKey(name);
    }

    public TreeNode? GetChild(string name) {
        if (!children.ContainsKey(name)) {
            return null;
        } 

        return children[name];
    }

    public void ForEachChild(Action<TreeNode> callback) {
        foreach (TreeNode child in children.Values) {
            callback.Invoke(child);
        }
    }

    public void InsertChild(Node child) {
        children.Add(child.Info.Name, new TreeNode(route, child, subscriber));
    }

    public void SubscribeToValue(Action<VariantValue> callback)
    {
        subscriber.RegisterSubscriber(id, callback);
    }

    public void UnsubscribeToValue(Action<VariantValue> callback)
    {
        subscriber.UnregisterSubscriber(id, callback);
    }

    public async Task<VariantValue> GetValue() {
        TaskCompletionSource<VariantValue> result = new TaskCompletionSource<VariantValue>();

        Action<VariantValue> callback = result.SetResult;

        subscriber.RegisterSubscriber(id, callback);
        VariantValue value = await result.Task;
        subscriber.UnregisterSubscriber(id, callback);

        return value;
    }

    public T ConvertVariantValue<T>(VariantValue value)
    {
        switch (valueType)
        {
            case CDPValueType.EDouble:
                return (T)(object)value.DValue;
            case CDPValueType.EFloat:
                return (T)(object)value.FValue;
            case CDPValueType.EUint64:
                return (T)(object)value.Ui64Value;
            case CDPValueType.EInt64:
                return (T)(object)value.I64Value;
            case CDPValueType.EUint:
                return (T)(object)value.UiValue;
            case CDPValueType.EInt:
                return (T)(object)value.IValue;
            case CDPValueType.EUshort:
                return (T)(object)value.UsValue;
            case CDPValueType.EShort:
                return (T)(object)value.SValue;
            case CDPValueType.EUchar:
                return (T)(object)value.UcValue;
            case CDPValueType.EChar:
                return (T)(object)value.CValue;
            case CDPValueType.EBool:
                return (T)(object)value.BValue;
            case CDPValueType.EString:
                return (T)(object)value.StrValue;
            default:
                throw new ArgumentException($"Cannot convert type '{valueType}' to '{typeof(T)}'");
        }
    }
}
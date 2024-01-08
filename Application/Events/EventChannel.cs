namespace CDPStudio.Events;

class EventChannel {
    public event EventHandler<Node>? OnStructure;
    public event EventHandler<VariantValue>? OnValue;

    public void RaiseStructure(Node node)
    {
        InternalRaiseStructure(node);
    }

    public void RaiseValue(VariantValue value)
    {
        InternalRaiseValue(value);
    }

    protected virtual void InternalRaiseStructure(Node node)
    {
        OnStructure?.Invoke(this, node);
    }

    protected virtual void InternalRaiseValue(VariantValue value)
    {
        OnValue?.Invoke(this, value);
    }
}
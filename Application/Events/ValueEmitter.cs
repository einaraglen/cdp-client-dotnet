namespace CDPStudio.Events;

class ValueEmitter {
    public event EventHandler<VariantValue>? OnEvent;

    public void Raise(VariantValue node)
    {
        InternalRaise(node);
    }

    protected virtual void InternalRaise(VariantValue node)
    {
        OnEvent?.Invoke(this, node);
    }
}
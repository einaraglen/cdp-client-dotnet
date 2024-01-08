namespace CDPStudio.Events;

class StructureEmitter
{
    public event EventHandler<Node>? OnEvent;

    public void Raise(Node node)
    {
        InternalRaise(node);
    }

    protected virtual void InternalRaise(Node node)
    {
        OnEvent?.Invoke(this, node);
    }
}
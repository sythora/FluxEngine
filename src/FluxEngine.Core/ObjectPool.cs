namespace FluxEngine.Core;

// holds dead objects so we stop hammering the gc. only worth it for stuff
// you spawn and kill a lot, bullets, particles, that kind of thing
public sealed class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _free = [];

    public int Available => _free.Count;

    // what the pool has ever made, handy in a debug overlay
    public int Created { get; private set; }

    public T Get()
    {
        if (_free.Count > 0)
            return _free.Pop();

        Created++;
        return new T();
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _free.Push(item);
    }

    public void Clear() => _free.Clear();
}

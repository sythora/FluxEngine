namespace FluxEngine.Core;

// states with enter/exit callbacks, beats a big switch in a corner somewhere
public sealed class StateMachine<TState> where TState : notnull
{
    private readonly Dictionary<TState, Action> _onEnter = [];
    private readonly Dictionary<TState, Action> _onExit = [];

    public StateMachine(TState initialState)
    {
        Current = initialState;
        Previous = initialState;
    }

    public TState Current { get; private set; }
    public TState Previous { get; private set; }

    public StateMachine<TState> OnEnter(TState state, Action callback)
    {
        _onEnter[state] = callback;
        return this;
    }

    public StateMachine<TState> OnExit(TState state, Action callback)
    {
        _onExit[state] = callback;
        return this;
    }

    public bool Change(TState next)
    {
        if (EqualityComparer<TState>.Default.Equals(next, Current))
            return false;

        if (_onExit.TryGetValue(Current, out var exit))
            exit();

        Previous = Current;
        Current = next;

        if (_onEnter.TryGetValue(next, out var enter))
            enter();

        return true;
    }
}

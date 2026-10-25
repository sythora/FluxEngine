using FluxEngine.Mathematics;

namespace FluxEngine.Core;

// everything you can hang off an entity. subclass this and override what you need
public abstract class Component
{
    // filled in by Entity.AddComponent
    public Entity Entity { get; internal set; } = null!;

    internal bool Started { get; set; }
    internal bool Destroyed { get; set; }

    // runs once before the first update
    public virtual void OnStart() { }

    // runs at a fixed rate, put physics here so it doesn't depend on fps
    public virtual void OnFixedUpdate(float deltaTime) { }

    // runs every frame
    public virtual void OnUpdate(float deltaTime) { }

    // draw world stuff here
    public virtual void OnDraw(Graphics.Renderer renderer) { }

    // removes just this component, entity stays alive
    public void Destroy() => Entity.RemoveComponent(this);

    public T? GetComponent<T>() where T : Component => Entity.GetComponent<T>();
}

// one thing in the world, holds components. no transform component because
// almost everything needs a position, so it just lives here
public sealed class Entity
{
    private readonly List<Component> _components = [];

    public Entity(string name = "entity")
    {
        Name = name;
    }

    public string Name { get; set; }
    public string Tag { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public Vector2 Position { get; set; }

    // degrees, clockwise because y goes down
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;

    public Scene? Scene { get; internal set; }
    public IReadOnlyList<Component> Components => _components;

    public T AddComponent<T>() where T : Component, new() => AddComponent(new T());

    public T AddComponent<T>(T component) where T : Component
    {
        ArgumentNullException.ThrowIfNull(component);

        if (component.Entity is not null || component.Destroyed)
            throw new InvalidOperationException("That component already belongs to an entity or has been destroyed.");

        component.Entity = this;
        _components.Add(component);

        // spawned mid game? then start it right now, not next scene start
        if (Scene is { IsRunning: true })
            StartComponent(component);

        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        foreach (var component in _components)
        {
            if (component is T match)
                return match;
        }

        return null;
    }

    public List<T> GetComponents<T>() where T : Component
    {
        var found = new List<T>();

        foreach (var component in _components)
        {
            if (component is T match)
                found.Add(match);
        }

        return found;
    }

    public bool HasComponent<T>() where T : Component => GetComponent<T>() is not null;

    // actual removal happens after the current loop so we don't stomp the list
    public void RemoveComponent(Component component)
    {
        if (component.Entity == this)
            component.Destroyed = true;
    }

    public void Destroy() => Scene?.Destroy(this);

    internal void Start()
    {
        // use index-based loop because OnStart can add new components,
        // which would break a foreach over the live list
        for (var i = 0; i < _components.Count; i++)
            StartComponent(_components[i]);
    }

    private void StartComponent(Component component)
    {
        if (component.Started || component.Destroyed)
            return;

        component.Started = true;
        component.OnStart();
    }

    internal void FixedUpdate(float fixedDeltaTime)
    {
        for (var i = 0; i < _components.Count; i++)
        {
            var component = _components[i];

            if (component.Destroyed)
                continue;

            component.OnFixedUpdate(fixedDeltaTime);
        }
    }

    internal void Update(float deltaTime)
    {
        for (var i = 0; i < _components.Count; i++)
        {
            var component = _components[i];

            if (component.Destroyed)
                continue;

            component.OnUpdate(deltaTime);
        }
    }

    internal void Draw(Graphics.Renderer renderer)
    {
        foreach (var component in _components)
        {
            if (component.Destroyed)
                continue;

            // sprites are sorted by layer, scene handles those separately
            if (component is Graphics.SpriteRenderer)
                continue;

            component.OnDraw(renderer);
        }
    }

    internal void DetachAllComponents()
    {
        foreach (var component in _components)
        {
            component.Entity = null!;
            component.Started = false;
            component.Destroyed = false;
        }

        _components.Clear();
    }

    internal void PruneComponents()
    {
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            var component = _components[i];
            if (!component.Destroyed)
                continue;

            if (component is FluxEngine.Physics.ColliderComponent collider)
                collider.OnRemovedFromScene();

            component.Entity = null!;
            _components.RemoveAt(i);
        }
    }
}

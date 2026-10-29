using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Core;

public sealed class Scene
{
    private readonly List<Entity> _entities = [];
    private readonly List<Entity> _destroyQueue = [];
    private readonly List<ISpriteDrawer> _spriteQueue = [];
    private readonly List<ColliderComponent> _colliders = [];

    public Color BackgroundColor { get; set; } = new(18, 22, 32);

    // null = world coordinates are screen coordinates
    public Camera? Camera { get; set; }

    // static level geometry. rigidbodies fall on top of these, they never move
    public List<BoxCollider> Solids { get; } = [];

    // all registered collider components (moving platforms, one-ways, etc)
    public IReadOnlyList<ColliderComponent> Colliders => _colliders;

    public LightingSystem Lighting { get; } = new();

    public bool IsRunning { get; private set; }
    public IReadOnlyList<Entity> Entities => _entities;

    public Entity Spawn(string name = "entity")
    {
        var entity = new Entity(name) { Scene = this };
        _entities.Add(entity);
        return entity;
    }

    public Entity Spawn(Vector2 position, string name = "entity")
    {
        var entity = Spawn(name);
        entity.Position = position;
        return entity;
    }

    public Entity Spawn(string name, Vector2 position) => Spawn(position, name);

    public void Destroy(Entity entity)
    {
        if (entity.Scene != this || _destroyQueue.Contains(entity))
            return;

        _destroyQueue.Add(entity);
    }

    public int EntityCount => _entities.Count;

    public int ComponentCount => _entities.Sum(entity => entity.Components.Count(component => !component.Destroyed));

    public Entity? Find(string name)
    {
        foreach (var entity in _entities)
        {
            if (entity.Name == name)
                return entity;
        }

        return null;
    }

    public List<Entity> FindByTag(string tag)
    {
        var found = new List<Entity>();

        foreach (var entity in _entities)
        {
            if (entity.Tag == tag)
                found.Add(entity);
        }

        return found;
    }

    public void Clear()
    {
        foreach (var entity in _entities)
        {
            foreach (var collider in entity.Components.OfType<ColliderComponent>())
                collider.OnRemovedFromScene();
            foreach (var light in entity.Components.OfType<Light2D>())
                light.OnRemovedFromScene();

            entity.DetachAllComponents();
            entity.Scene = null;
        }

        _entities.Clear();
        _destroyQueue.Clear();
        _colliders.Clear();
        Solids.Clear();
        Lighting.Clear();
        IsRunning = false;
    }

    internal void RegisterCollider(ColliderComponent collider)
    {
        if (!_colliders.Contains(collider))
            _colliders.Add(collider);
    }

    internal void UnregisterCollider(ColliderComponent collider)
    {
        _colliders.Remove(collider);
    }

    // these run automatically when you use Engine.Run(scene), but they are
    // public in case you want to drive the scene yourself
    public void Start()
    {
        IsRunning = true;

        for (var i = 0; i < _entities.Count; i++)
            _entities[i].Start();
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];

            if (!entity.Enabled)
                continue;

            entity.FixedUpdate(fixedDeltaTime);
            entity.PruneComponents();
        }

        ApplyDestroyQueue();
    }

    public void Update(float deltaTime)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];

            if (!entity.Enabled)
                continue;

            entity.Update(deltaTime);
            entity.PruneComponents();
        }

        Camera?.Update(deltaTime);
        Lighting.Update(deltaTime);
        ApplyDestroyQueue();
    }

    public void Draw(Renderer renderer)
    {
        renderer.Camera = Camera;

        _spriteQueue.Clear();

        foreach (var entity in _entities)
        {
            if (!entity.Enabled)
                continue;

            entity.Draw(renderer);

            foreach (var component in entity.Components)
            {
                if (component is ISpriteDrawer drawer && !component.Destroyed)
                    _spriteQueue.Add(drawer);
            }
        }

        // higher layer means drawn later, so it sits on top
        _spriteQueue.Sort((a, b) => a.Layer.CompareTo(b.Layer));

        foreach (var drawer in _spriteQueue)
            drawer.OnDraw(renderer);

        if (Lighting.Enabled)
            Lighting.Draw(renderer, Camera);
    }

    private void ApplyDestroyQueue()
    {
        if (_destroyQueue.Count == 0)
            return;

        foreach (var entity in _destroyQueue)
        {
            if (!_entities.Remove(entity))
                continue;

            foreach (var component in entity.Components)
            {
                if (component is ColliderComponent collider)
                    collider.OnRemovedFromScene();
                if (component is Light2D light)
                    light.OnRemovedFromScene();
            }

            entity.DetachAllComponents();
            entity.Scene = null;
        }

        _destroyQueue.Clear();
    }
}

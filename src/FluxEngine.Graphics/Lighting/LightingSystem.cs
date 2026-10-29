using FluxEngine.Mathematics;

namespace FluxEngine.Graphics.Lighting;

public readonly record struct StandaloneLight(Vector2 Position, float Radius, Color Color, float Intensity);

public sealed class LightingSystem
{
    private readonly List<Light2D> _lights = [];
    private readonly List<StandaloneLight> _standaloneLights = [];

    public bool Enabled { get; set; }

    public DayNightCycle DayNight { get; } = new();

    public bool UseDayNightCycle { get; set; } = true;

    public Color ManualAmbientColor { get; set; } = Color.White;

    public Color AmbientColor => UseDayNightCycle ? DayNight.AmbientColor : ManualAmbientColor;

    public IReadOnlyList<Light2D> Lights => _lights;

    public void RegisterLight(Light2D light)
    {
        ArgumentNullException.ThrowIfNull(light);
        if (!_lights.Contains(light))
            _lights.Add(light);
    }

    public void UnregisterLight(Light2D light)
    {
        _lights.Remove(light);
    }

    /// <summary>
    /// Adds a transient light for one frame (ideal for particles, muzzle flashes, or projectile explosions).
    /// </summary>
    public void AddStandaloneLight(Vector2 position, float radius, Color color, float intensity = 1.0f)
    {
        if (radius <= 0f || intensity <= 0f)
            return;

        _standaloneLights.Add(new StandaloneLight(position, radius, color, intensity));
    }

    public void Clear()
    {
        _lights.Clear();
        _standaloneLights.Clear();
    }

    public void Update(float deltaTime)
    {
        if (UseDayNightCycle)
            DayNight.Update(deltaTime);
    }

    public void Draw(Renderer renderer, Camera? camera)
    {
        if (!Enabled)
        {
            _standaloneLights.Clear();
            return;
        }

        var ambient = AmbientColor;
        if (!renderer.BeginLightmap(ambient))
        {
            _standaloneLights.Clear();
            return;
        }

        for (var i = 0; i < _lights.Count; i++)
        {
            var light = _lights[i];
            if (!light.Enabled || light.Destroyed || light.Intensity <= 0f)
                continue;

            if (light.Entity is not null && !light.Entity.Enabled)
                continue;

            var radius = light.EffectiveRadius;
            if (radius <= 0f)
                continue;

            renderer.DrawLight(light.WorldPosition, radius, light.Color, light.Intensity);
        }

        for (var i = 0; i < _standaloneLights.Count; i++)
        {
            var light = _standaloneLights[i];
            renderer.DrawLight(light.Position, light.Radius, light.Color, light.Intensity);
        }

        _standaloneLights.Clear();
        renderer.EndLightmap();
    }
}

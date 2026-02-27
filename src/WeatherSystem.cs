using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public enum WeatherType : byte
{
    Clear = 0,
    Rain,
    Snow,
    Fog
}

public class WeatherSystem
{
    private readonly ParticleSystem _particles;
    private readonly LightingSystem _lighting;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Random _rng = new();
    private AudioSystem _audio;

    private WeatherType _currentWeather = WeatherType.Clear;
    private WeatherType _targetWeather = WeatherType.Clear;
    private float _transitionProgress = 1f;
    private float _intensity;

    private bool _isNight;

    private Texture2D _fogTexture;
    private float _fogOpacity;
    private float _fogTime;
    private Vector2 _cameraPos;
    private Vector2 _lastCameraPos;
    private Vector2 _cameraDelta;

    public float Wetness { get; private set; }
    public WeatherType CurrentWeather => _targetWeather;
    public int ParticleCount => _particles.ActiveCount;

    public WeatherSystem(GraphicsDevice graphicsDevice, ParticleSystem particles, LightingSystem lighting)
    {
        _graphicsDevice = graphicsDevice;
        _particles = particles;
        _lighting = lighting;
    }

    public void SetAudioSystem(AudioSystem audio) => _audio = audio;

    public void LoadContent()
    {
        _fogTexture = CreateFogTexture();
    }

    public void SetWeather(WeatherType weather)
    {
        if (weather == _targetWeather) return;
        _targetWeather = weather;
        _transitionProgress = 0f;
        _audio?.SetAmbient(weather);
    }

    public void SetDayNight(bool isNight)
    {
        _isNight = isNight;
        _audio?.SetDayNight(isNight);
    }

    public void Update(float deltaTime, Vector2 cameraPosition)
    {
        // Compensate particles for camera movement
        _cameraDelta = cameraPosition - _lastCameraPos;
        _cameraPos = cameraPosition;
        _lastCameraPos = cameraPosition;
        _particles.OffsetAll(-_cameraDelta.X, -_cameraDelta.Y);

        // Advance transition
        if (_transitionProgress < 1f)
        {
            _transitionProgress = MathF.Min(1f, _transitionProgress + deltaTime / GameConfig.WeatherTransitionSeconds);
            if (_transitionProgress >= 1f)
                _currentWeather = _targetWeather;
        }

        // Calculate effective intensity with ease-in curve (t² for gradual buildup)
        float targetIntensity = (_targetWeather != WeatherType.Clear) ? 1f : 0f;
        float currentIntensity = (_currentWeather != WeatherType.Clear) ? 1f : 0f;
        float easedProgress = _transitionProgress * _transitionProgress;
        _intensity = MathHelper.Lerp(currentIntensity, targetIntensity, easedProgress);

        // Update audio intensity
        _audio?.UpdateWeatherIntensity(_intensity);

        // Update wetness
        if (_targetWeather == WeatherType.Rain || _currentWeather == WeatherType.Rain)
            Wetness = MathF.Min(1f, Wetness + deltaTime * GameConfig.WetnessGainRate * _intensity);
        else
            Wetness = MathF.Max(0f, Wetness - deltaTime * GameConfig.WetnessDryRate);

        // Update fog opacity
        bool isFoggy = (_currentWeather == WeatherType.Fog || _targetWeather == WeatherType.Fog);
        float targetFog = isFoggy ? GameConfig.FogMaxOpacity * _intensity : 0f;
        _fogOpacity = MathHelper.Lerp(_fogOpacity, targetFog, deltaTime * 3f);

        // Animate fog scroll
        _fogTime += deltaTime;

        UpdateAmbientLighting();
        EmitParticles(deltaTime);
    }

    private void UpdateAmbientLighting()
    {
        Color baseAmbient = _isNight ? GameConfig.AmbientNight : GameConfig.AmbientDay;

        if (_intensity > 0f)
        {
            WeatherType effectiveWeather = _transitionProgress < 1f ? _targetWeather : _currentWeather;
            Color weatherAmbient = effectiveWeather switch
            {
                WeatherType.Rain => _isNight
                    ? LerpColor(GameConfig.AmbientNight, GameConfig.AmbientRain, 0.5f)
                    : GameConfig.AmbientRain,
                WeatherType.Snow => _isNight
                    ? LerpColor(GameConfig.AmbientNight, GameConfig.AmbientSnow, 0.5f)
                    : GameConfig.AmbientSnow,
                WeatherType.Fog => _isNight
                    ? LerpColor(GameConfig.AmbientNight, GameConfig.AmbientFog, 0.3f)
                    : GameConfig.AmbientFog,
                _ => baseAmbient
            };

            _lighting.AmbientColor = LerpColor(baseAmbient, weatherAmbient, _intensity);
        }
        else
        {
            _lighting.AmbientColor = baseAmbient;
        }
    }

    private void EmitParticles(float deltaTime)
    {
        WeatherType activeType = _transitionProgress < 1f ? _targetWeather : _currentWeather;
        float margin = GameConfig.ParticleSpawnMargin;
        float spawnWidth = GameConfig.ScreenWidth + 2 * margin;

        switch (activeType)
        {
            case WeatherType.Rain:
            {
                int emitCount = (int)(GameConfig.RainParticlesPerSecond * deltaTime * _intensity);
                for (int i = 0; i < emitCount; i++)
                {
                    float x = -margin + _rng.NextSingle() * spawnWidth;
                    float y = -GameConfig.RainTexHeight;
                    float vx = GameConfig.RainVelocityX + (_rng.NextSingle() - 0.5f) * 20f;
                    float vy = GameConfig.RainVelocityY + _rng.NextSingle() * 60f;
                    float life = GameConfig.ScreenHeight / vy * 1.2f;
                    _particles.Emit(x, y, vx, vy, life, 1f, 0);
                }
                EmitCameraFill(0, deltaTime);
                break;
            }
            case WeatherType.Snow:
            {
                int emitCount = (int)(GameConfig.SnowParticlesPerSecond * deltaTime * _intensity);
                for (int i = 0; i < emitCount; i++)
                {
                    float x = -margin + _rng.NextSingle() * spawnWidth;
                    float y = -GameConfig.SnowTexSize;
                    float vx = GameConfig.SnowVelocityX + (_rng.NextSingle() - 0.5f) * 30f;
                    float vy = GameConfig.SnowVelocityY + _rng.NextSingle() * 20f;
                    float life = GameConfig.ScreenHeight / vy * 1.5f;
                    float size = 0.6f + _rng.NextSingle() * 0.8f;
                    _particles.Emit(x, y, vx, vy, life, size, 1);
                }
                EmitCameraFill(1, deltaTime);
                break;
            }
        }
    }

    /// <summary>
    /// When camera moves, emit fill particles at random Y positions in the
    /// newly exposed screen strip so coverage is immediate.
    /// </summary>
    private void EmitCameraFill(byte type, float deltaTime)
    {
        float absDx = MathF.Abs(_cameraDelta.X);
        float absDy = MathF.Abs(_cameraDelta.Y);
        if (absDx < 1f && absDy < 1f) return;

        // Steady-state density: particles per screen pixel²
        float density;
        if (type == 0) // rain
        {
            float avgVy = GameConfig.RainVelocityY + 30f;
            float avgLife = GameConfig.ScreenHeight / avgVy * 1.2f;
            density = GameConfig.RainParticlesPerSecond * avgLife
                      / (GameConfig.ScreenWidth * GameConfig.ScreenHeight) * _intensity;
        }
        else // snow
        {
            float avgVy = GameConfig.SnowVelocityY + 10f;
            float avgLife = GameConfig.ScreenHeight / avgVy * 1.5f;
            float steadyState = MathF.Min(GameConfig.SnowParticlesPerSecond * avgLife, GameConfig.MaxParticles);
            density = steadyState
                      / (GameConfig.ScreenWidth * GameConfig.ScreenHeight) * _intensity;
        }

        // Fill horizontal strip
        if (absDx > 1f)
        {
            int count = (int)(density * absDx * GameConfig.ScreenHeight);
            float stripX = _cameraDelta.X > 0
                ? GameConfig.ScreenWidth - absDx
                : 0;
            for (int i = 0; i < count; i++)
                EmitFillParticle(type, stripX + _rng.NextSingle() * absDx, _rng.NextSingle() * GameConfig.ScreenHeight);
        }

        // Fill vertical strip
        if (absDy > 1f)
        {
            int count = (int)(density * absDy * GameConfig.ScreenWidth);
            float stripY = _cameraDelta.Y > 0
                ? GameConfig.ScreenHeight - absDy
                : 0;
            for (int i = 0; i < count; i++)
                EmitFillParticle(type, _rng.NextSingle() * GameConfig.ScreenWidth, stripY + _rng.NextSingle() * absDy);
        }
    }

    private void EmitFillParticle(byte type, float x, float y)
    {
        if (type == 0) // rain
        {
            float vx = GameConfig.RainVelocityX + (_rng.NextSingle() - 0.5f) * 20f;
            float vy = GameConfig.RainVelocityY + _rng.NextSingle() * 60f;
            float fullLife = (GameConfig.ScreenHeight + GameConfig.RainTexHeight) / vy * 1.2f;
            float elapsed = (y + GameConfig.RainTexHeight) / vy;
            float remainLife = MathF.Max(0.1f, fullLife - elapsed);
            _particles.Emit(x, y, vx, vy, remainLife, fullLife, 1f, 0);
        }
        else // snow
        {
            float vx = GameConfig.SnowVelocityX + (_rng.NextSingle() - 0.5f) * 30f;
            float vy = GameConfig.SnowVelocityY + _rng.NextSingle() * 20f;
            float fullLife = (GameConfig.ScreenHeight + GameConfig.SnowTexSize) / vy * 1.5f;
            float elapsed = (y + GameConfig.SnowTexSize) / vy;
            float remainLife = MathF.Max(0.1f, fullLife - elapsed);
            float size = 0.6f + _rng.NextSingle() * 0.8f;
            _particles.Emit(x, y, vx, vy, remainLife, fullLife, size, 1);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Fog overlay — two scrolling noise layers for non-uniform density
        if (_fogOpacity > 0.01f)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null, Matrix.Identity);
            byte fogAlpha = (byte)(_fogOpacity * 255);
            var fogTint = new Color(GameConfig.FogColor.R, GameConfig.FogColor.G, GameConfig.FogColor.B, fogAlpha);

            // Wind-driven scroll (both layers drift in same direction, different speeds)
            float windX = _fogTime * GameConfig.FogWindSpeed * GameConfig.FogWindDirection.X;
            float windY = _fogTime * GameConfig.FogWindSpeed * GameConfig.FogWindDirection.Y;

            // Layer 1: medium-scale patches, full wind speed
            int srcW1 = GameConfig.ScreenWidth / 2;
            int srcH1 = GameConfig.ScreenHeight / 2;
            int scrollX1 = (int)((windX + _cameraPos.X * 0.9f) / 2f);
            int scrollY1 = (int)((windY + _cameraPos.Y * 0.9f) / 2f);
            spriteBatch.Draw(
                _fogTexture,
                new Rectangle(0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight),
                new Rectangle(scrollX1, scrollY1, srcW1, srcH1),
                fogTint
            );

            // Layer 2: large-scale features, slower wind (deeper layer parallax)
            int srcW2 = GameConfig.ScreenWidth / 4;
            int srcH2 = GameConfig.ScreenHeight / 4;
            int scrollX2 = (int)((windX * 0.4f + _cameraPos.X * 0.9f) / 4f) + 67;
            int scrollY2 = (int)((windY * 0.4f + _cameraPos.Y * 0.9f) / 4f) + 41;
            byte alpha2 = (byte)(fogAlpha * 0.6f);
            var fogTint2 = new Color(GameConfig.FogColor.R, GameConfig.FogColor.G, GameConfig.FogColor.B, alpha2);
            spriteBatch.Draw(
                _fogTexture,
                new Rectangle(0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight),
                new Rectangle(scrollX2, scrollY2, srcW2, srcH2),
                fogTint2
            );

            spriteBatch.End();
        }

        // Particles
        if (_particles.ActiveCount > 0)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                null, null, null,
                Matrix.Identity
            );
            _particles.Draw(spriteBatch);
            spriteBatch.End();
        }
    }

    // --- Fog texture generation: tileable value noise ---

    private Texture2D CreateFogTexture()
    {
        const int size = 512;
        var data = new Color[size * size];

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                // Three octaves: large blobs + medium detail + fine detail
                float v1 = SampleValueNoise(px, py, size, 3, 42);
                float v2 = SampleValueNoise(px, py, size, 7, 137);
                float v3 = SampleValueNoise(px, py, size, 13, 251);
                float value = v1 * 0.55f + v2 * 0.3f + v3 * 0.15f;

                // Contrast curve: distinct patches with clear gaps between them
                value = SmoothstepClamp(0.3f, 0.7f, value);

                byte a = (byte)(Math.Clamp(value, 0f, 1f) * 255);
                data[py * size + px] = new Color(255, 255, 255, a);
            }
        }

        var tex = new Texture2D(_graphicsDevice, size, size);
        tex.SetData(data);
        return tex;
    }

    private static float SmoothstepClamp(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static float SampleValueNoise(int px, int py, int texSize, int gridSize, int seed)
    {
        float gx = (float)px / texSize * gridSize;
        float gy = (float)py / texSize * gridSize;
        int x0 = (int)gx;
        int y0 = (int)gy;
        float fx = gx - x0;
        float fy = gy - y0;

        // Smoothstep interpolation
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);

        // Hash-based grid values with wrapping for seamless tiling
        float v00 = HashToFloat(x0 % gridSize, y0 % gridSize, seed);
        float v10 = HashToFloat((x0 + 1) % gridSize, y0 % gridSize, seed);
        float v01 = HashToFloat(x0 % gridSize, (y0 + 1) % gridSize, seed);
        float v11 = HashToFloat((x0 + 1) % gridSize, (y0 + 1) % gridSize, seed);

        return MathHelper.Lerp(
            MathHelper.Lerp(v00, v10, fx),
            MathHelper.Lerp(v01, v11, fx),
            fy
        );
    }

    private static float HashToFloat(int x, int y, int seed)
    {
        int h = x * 374761393 + y * 668265263 + seed;
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (int)MathHelper.Lerp(a.R, b.R, t),
            (int)MathHelper.Lerp(a.G, b.G, t),
            (int)MathHelper.Lerp(a.B, b.B, t)
        );
    }
}

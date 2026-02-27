using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class ParticleSystem
{
    private readonly Particle[] _particles;
    private int _activeCount;
    private readonly Texture2D[] _textures;
    private readonly GraphicsDevice _graphicsDevice;

    public ParticleSystem(GraphicsDevice graphicsDevice, int maxParticles)
    {
        _graphicsDevice = graphicsDevice;
        _particles = new Particle[maxParticles];
        _activeCount = 0;
        _textures = new Texture2D[2];
    }

    public int ActiveCount => _activeCount;

    public void LoadContent()
    {
        _textures[0] = CreateRainTexture();
        _textures[1] = CreateSnowTexture();
    }

    public bool Emit(float x, float y, float vx, float vy, float life, float size, byte type)
    {
        return Emit(x, y, vx, vy, life, life, size, type);
    }

    public bool Emit(float x, float y, float vx, float vy, float life, float maxLife, float size, byte type)
    {
        if (_activeCount >= _particles.Length) return false;
        _particles[_activeCount++] = new Particle
        {
            X = x, Y = y,
            VelocityX = vx, VelocityY = vy,
            Life = life, MaxLife = maxLife,
            Size = size, Type = type
        };
        return true;
    }

    public void OffsetAll(float dx, float dy)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            _particles[i].X += dx;
            _particles[i].Y += dy;
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            ref var p = ref _particles[i];
            p.X += p.VelocityX * deltaTime;
            p.Y += p.VelocityY * deltaTime;
            p.Life -= deltaTime;

            // Remove dead or far off-screen particles
            if (p.Life <= 0f
                || p.X < -GameConfig.ParticleSpawnMargin || p.X > GameConfig.ScreenWidth + GameConfig.ParticleSpawnMargin
                || p.Y > GameConfig.ScreenHeight + 200)
            {
                _particles[i] = _particles[--_activeCount];
                i--;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            ref var p = ref _particles[i];
            float alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            var tint = new Color(255, 255, 255, (byte)(alpha * 200));
            var tex = _textures[p.Type];

            spriteBatch.Draw(
                tex,
                new Vector2(p.X, p.Y),
                null,
                tint,
                0f,
                Vector2.Zero,
                p.Size,
                SpriteEffects.None,
                0f
            );
        }
    }

    private Texture2D CreateRainTexture()
    {
        int w = GameConfig.RainTexWidth;
        int h = GameConfig.RainTexHeight;
        var tex = new Texture2D(_graphicsDevice, w, h);
        var data = new Color[w * h];
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                float t = 1f - MathF.Abs(py - h / 2f) / (h / 2f);
                byte a = (byte)(t * 255);
                data[py * w + px] = new Color(200, 220, 255, a);
            }
        }
        tex.SetData(data);
        return tex;
    }

    private Texture2D CreateSnowTexture()
    {
        int s = GameConfig.SnowTexSize;
        var tex = new Texture2D(_graphicsDevice, s, s);
        var data = new Color[s * s];
        float center = s / 2f;
        for (int py = 0; py < s; py++)
        {
            for (int px = 0; px < s; px++)
            {
                float dx = (px + 0.5f - center) / center;
                float dy = (py + 0.5f - center) / center;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                byte a = dist < 1f ? (byte)((1f - dist) * 255) : (byte)0;
                data[py * s + px] = new Color(240, 245, 255, a);
            }
        }
        tex.SetData(data);
        return tex;
    }
}

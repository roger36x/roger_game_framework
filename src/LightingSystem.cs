using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class LightingSystem
{
    private readonly GraphicsDevice _graphicsDevice;

    // Light map
    private RenderTarget2D _lightMap;

    // Blob texture for soft light rendering
    private Texture2D _lightBlobTexture;
    private static int BlobWidth => GameConfig.LightBlobWidth;
    private static int BlobHeight => GameConfig.LightBlobHeight;

    // Blend states
    private BlendState _additiveBlend;
    private BlendState _multiplyBlend;

    // Lights
    private readonly List<LightSource> _lights = new();

    // Ambient
    private Color _ambientColor = GameConfig.AmbientNight;

    public Color AmbientColor
    {
        get => _ambientColor;
        set => _ambientColor = value;
    }

    public int LightCount => _lights.Count;

    public LightingSystem(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent()
    {
        _lightMap = new RenderTarget2D(
            _graphicsDevice,
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );

        _lightBlobTexture = CreateLightBlobTexture();

        _additiveBlend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One
        };

        _multiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero
        };
    }

    public void AddLight(LightSource light)
    {
        _lights.Add(light);
    }

    public void ClearLights()
    {
        _lights.Clear();
    }

    /// <summary>
    /// Renders the light map to the internal RenderTarget2D.
    /// Call this after drawing the scene to the backbuffer.
    /// </summary>
    public void DrawLightMap(SpriteBatch spriteBatch, Camera camera, ChunkManager chunkManager)
    {
        _graphicsDevice.SetRenderTarget(_lightMap);
        _graphicsDevice.Clear(_ambientColor);

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            _additiveBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            camera.GetTransformMatrix()
        );

        for (int i = 0; i < _lights.Count; i++)
        {
            DrawPointLight(spriteBatch, _lights[i], chunkManager);
        }

        spriteBatch.End();
        _graphicsDevice.SetRenderTarget(null);
    }

    /// <summary>
    /// Draws the light map over the backbuffer using multiply blending.
    /// </summary>
    public void ApplyLightMap(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            _multiplyBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            Matrix.Identity
        );

        spriteBatch.Draw(_lightMap, Vector2.Zero, Color.White);
        spriteBatch.End();
    }

    private void DrawPointLight(SpriteBatch spriteBatch, LightSource light, ChunkManager chunkManager)
    {
        int centerX = (int)MathF.Round(light.TilePosition.X);
        int centerY = (int)MathF.Round(light.TilePosition.Y);
        int r = light.Radius;
        float rSq = r * r;

        for (int ty = centerY - r; ty <= centerY + r; ty++)
        {
            for (int tx = centerX - r; tx <= centerX + r; tx++)
            {
                float dx = tx - light.TilePosition.X;
                float dy = ty - light.TilePosition.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq > rSq) continue;

                // Shadow check
                if (!HasLineOfSight(centerX, centerY, tx, ty, chunkManager))
                    continue;

                float dist = MathF.Sqrt(distSq);
                float normalized = dist / r;
                float falloff = (1f - normalized) * (1f - normalized) * light.Intensity;

                var tint = new Color(
                    (int)(light.Color.R * falloff),
                    (int)(light.Color.G * falloff),
                    (int)(light.Color.B * falloff)
                );

                Vector2 screenPos = IsoUtils.WorldToScreen(tx, ty);
                var blobPos = new Vector2(
                    screenPos.X - BlobWidth / 2f,
                    screenPos.Y - BlobHeight / 2f + IsoUtils.TileHeight / 2f
                );

                spriteBatch.Draw(_lightBlobTexture, blobPos, tint);
            }
        }
    }

    /// <summary>
    /// Bresenham line-of-sight check. Returns true if no block obstructs the path.
    /// Checks intermediate tiles only (excludes start and end).
    /// </summary>
    private static bool HasLineOfSight(int x0, int y0, int x1, int y1, ChunkManager chunkManager)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int cx = x0;
        int cy = y0;

        while (cx != x1 || cy != y1)
        {
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                cx += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                cy += sy;
            }

            // Skip the final tile (we want to light walls, not the space behind them)
            if (cx == x1 && cy == y1) break;

            // Check for blocking
            var cell = chunkManager.GetCell(cx, cy);
            if (cell.BlockHeight > 0)
                return false;
        }

        return true;
    }

    private Texture2D CreateLightBlobTexture()
    {
        var texture = new Texture2D(_graphicsDevice, BlobWidth, BlobHeight);
        var data = new Color[BlobWidth * BlobHeight];

        float halfW = BlobWidth / 2f;
        float halfH = BlobHeight / 2f;

        for (int py = 0; py < BlobHeight; py++)
        {
            for (int px = 0; px < BlobWidth; px++)
            {
                float dx = (px - halfW) / halfW;
                float dy = (py - halfH) / halfH;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                byte alpha;
                if (dist >= 1f)
                {
                    alpha = 0;
                }
                else
                {
                    float f = (1f - dist) * (1f - dist); // quadratic falloff
                    alpha = (byte)(f * 255);
                }

                data[py * BlobWidth + px] = new Color(255, 255, 255, alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }
}

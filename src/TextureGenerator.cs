using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public static class TextureGenerator
{
    private static readonly Dictionary<string, Texture2D> _cache = new();

    public static void ClearCache()
    {
        foreach (var tex in _cache.Values)
            tex.Dispose();
        _cache.Clear();
    }

    public static Texture2D GetOrCreate(GraphicsDevice gd, EntityTemplate template)
    {
        if (_cache.TryGetValue(template.Name, out var cached))
            return cached;

        var tex = template.Sprite.Shape switch
        {
            ShapeType.Block => CreateBlockTexture(gd, template.Sprite.Width, template.Sprite.Height, template.Colors),
            ShapeType.Diamond => CreateDiamondTexture(gd, template.Sprite.Width, template.Colors.Top),
            ShapeType.Rect => CreateRectTexture(gd, template.Sprite.Width, template.Sprite.Height, template.Colors.Top),
            _ => CreateRectTexture(gd, template.Sprite.Width, template.Sprite.Height, template.Colors.Top)
        };

        _cache[template.Name] = tex;
        return tex;
    }

    public static Texture2D GetCached(string templateName)
    {
        _cache.TryGetValue(templateName, out var tex);
        return tex;
    }

    public static Texture2D CreateBlockTexture(GraphicsDevice device, int texW, int texH, ColorTemplate colors)
    {
        var texture = new Texture2D(device, texW, texH);
        var data = new Color[texW * texH];

        var topColor = colors.Top;
        var leftColor = colors.Left;
        var rightColor = colors.Right;
        var borderColor = new Color(
            (int)(topColor.R * 0.3f),
            (int)(topColor.G * 0.3f),
            (int)(topColor.B * 0.3f),
            topColor.A);

        float halfW = texW / 2f;

        for (int py = 0; py < texH; py++)
        {
            for (int px = 0; px < texW; px++)
            {
                Color c = Color.Transparent;

                // Top face diamond
                float dxTop = MathF.Abs(px - halfW) / halfW;
                float dyTop = MathF.Abs(py - 16f) / 16f;
                if (dxTop + dyTop <= 1f && py >= 0 && py <= 32)
                {
                    c = (dxTop + dyTop > 0.9f) ? borderColor : topColor;
                }

                // Left face
                if (c == Color.Transparent && px >= 0 && px < 32)
                {
                    float topEdgeY = 16f + 0.5f * px;
                    float bottomEdgeY = 32f + 0.5f * px;
                    if (py >= topEdgeY && py <= bottomEdgeY)
                    {
                        bool isBorder = py <= topEdgeY + 1 || py >= bottomEdgeY - 1 || px <= 1;
                        c = isBorder ? borderColor : leftColor;
                    }
                }

                // Right face
                if (c == Color.Transparent && px >= 32 && px < 64)
                {
                    float topEdgeY = 48f - 0.5f * px;
                    float bottomEdgeY = 64f - 0.5f * px;
                    if (py >= topEdgeY && py <= bottomEdgeY)
                    {
                        bool isBorder = py <= topEdgeY + 1 || py >= bottomEdgeY - 1 || px >= 62;
                        c = isBorder ? borderColor : rightColor;
                    }
                }

                data[py * texW + px] = c;
            }
        }

        texture.SetData(data);
        return texture;
    }

    public static Texture2D CreateDiamondTexture(GraphicsDevice device, int size, Color color)
    {
        var texture = new Texture2D(device, size, size);
        var data = new Color[size * size];

        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = MathF.Abs(x - center) / center;
                float dy = MathF.Abs(y - center) / center;
                if (dx + dy <= 1f)
                {
                    bool isBorder = dx + dy > 0.7f;
                    data[y * size + x] = isBorder
                        ? new Color((int)(color.R * 0.6f), (int)(color.G * 0.6f), (int)(color.B * 0.6f))
                        : color;
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }

        texture.SetData(data);
        return texture;
    }

    public static Texture2D CreateRectTexture(GraphicsDevice device, int w, int h, Color fillColor)
    {
        var texture = new Texture2D(device, w, h);
        var data = new Color[w * h];

        var borderColor = new Color(
            (int)(fillColor.R * 0.5f),
            (int)(fillColor.G * 0.5f),
            (int)(fillColor.B * 0.5f),
            fillColor.A);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                data[y * w + x] = isBorder ? borderColor : fillColor;
            }
        }

        texture.SetData(data);
        return texture;
    }
}

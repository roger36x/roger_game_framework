using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public struct TileCell
{
    /// <summary>0 or 1, used for checkerboard pattern.</summary>
    public byte GroundType;

    /// <summary>0 = no block, 1+ = block height in layers.</summary>
    public int BlockHeight;
}

/// <summary>
/// Provides ground tile textures. Map data is now managed by ChunkManager.
/// </summary>
public class TileMap
{
    private Texture2D _tileLightTexture;
    private Texture2D _tileDarkTexture;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _tileLightTexture = CreateDiamondTexture(graphicsDevice, IsoUtils.TileWidth, IsoUtils.TileHeight, GameConfig.TileLightColor);
        _tileDarkTexture = CreateDiamondTexture(graphicsDevice, IsoUtils.TileWidth, IsoUtils.TileHeight, GameConfig.TileDarkColor);
    }

    public Texture2D GetGroundTexture(int groundType)
    {
        return groundType == 0 ? _tileLightTexture : _tileDarkTexture;
    }

    private static Texture2D CreateDiamondTexture(GraphicsDevice device, int width, int height, Color fillColor)
    {
        var texture = new Texture2D(device, width, height);
        var data = new Color[width * height];

        float centerX = width / 2f;
        float centerY = height / 2f;

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                float dx = MathF.Abs(px - centerX) / centerX;
                float dy = MathF.Abs(py - centerY) / centerY;
                if (dx + dy <= 1f)
                {
                    if (dx + dy > 0.9f)
                        data[py * width + px] = new Color(
                            (int)(fillColor.R * 0.7f),
                            (int)(fillColor.G * 0.7f),
                            (int)(fillColor.B * 0.7f)
                        );
                    else
                        data[py * width + px] = fillColor;
                }
                else
                {
                    data[py * width + px] = Color.Transparent;
                }
            }
        }

        texture.SetData(data);
        return texture;
    }
}

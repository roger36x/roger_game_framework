using System;
using Microsoft.Xna.Framework;

namespace Game;

public static class IsoUtils
{
    public static int TileWidth => GameConfig.TileWidth;
    public static int TileHeight => GameConfig.TileHeight;
    public static int BlockVisualHeight => GameConfig.BlockVisualHeight;

    /// <summary>
    /// Converts tile grid coordinates to screen pixel position.
    /// Returns the top-center of the diamond tile.
    /// </summary>
    public static Vector2 WorldToScreen(float tileX, float tileY)
    {
        float screenX = (tileX - tileY) * (TileWidth / 2f);
        float screenY = (tileX + tileY) * (TileHeight / 2f);
        return new Vector2(screenX, screenY);
    }

    /// <summary>
    /// Converts tile grid coordinates with height to screen pixel position.
    /// Height shifts the sprite upward on screen.
    /// </summary>
    public static Vector2 WorldToScreen(float tileX, float tileY, float height)
    {
        var pos = WorldToScreen(tileX, tileY);
        pos.Y -= height * BlockVisualHeight;
        return pos;
    }

    /// <summary>
    /// Computes a depth sorting key for the painter's algorithm.
    /// Higher key = drawn later = appears in front.
    /// drawLayer: 0=ground tile, 1=block, 2=entity
    /// </summary>
    public static float GetDepthKey(float tileX, float tileY, float height, int drawLayer)
    {
        return (tileX + tileY) + height * 0.01f + drawLayer * 0.001f;
    }

    /// <summary>
    /// Converts screen pixel position to tile grid coordinates.
    /// </summary>
    public static (int tileX, int tileY) ScreenToWorld(Vector2 screenPos)
    {
        float tileX = (screenPos.X / (TileWidth / 2f) + screenPos.Y / (TileHeight / 2f)) / 2f;
        float tileY = (screenPos.Y / (TileHeight / 2f) - screenPos.X / (TileWidth / 2f)) / 2f;
        return ((int)MathF.Floor(tileX), (int)MathF.Floor(tileY));
    }
}

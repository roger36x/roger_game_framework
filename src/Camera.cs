using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class Camera
{
    public Vector2 Position;

    private readonly Viewport _viewport;
    private Vector2 _velocity;
    private static float SmoothTime => GameConfig.CameraSmoothTime;

    public Camera(Viewport viewport)
    {
        _viewport = viewport;
    }

    public void Follow(Vector2 target, float deltaTime)
    {
        // Critically damped spring (same as Unity's SmoothDamp)
        float omega = 2f / SmoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        Vector2 diff = Position - target;
        Vector2 temp = (_velocity + omega * diff) * deltaTime;
        _velocity = (_velocity - omega * temp) * exp;
        Position = target + (diff + temp) * exp;
    }

    public Matrix GetTransformMatrix()
    {
        // Round to integer pixels to prevent sub-pixel flickering with PointClamp
        return Matrix.CreateTranslation(
            MathF.Round(-Position.X + _viewport.Width / 2f),
            MathF.Round(-Position.Y + _viewport.Height / 2f),
            0f
        );
    }

    /// <summary>
    /// Returns the bounding box of visible tiles in tile-space coordinates.
    /// Transforms all 4 viewport corners to tile coords and takes the AABB.
    /// </summary>
    public (int minX, int minY, int maxX, int maxY) GetVisibleTileBounds(int padding = 2)
    {
        float offsetX = Position.X - _viewport.Width / 2f;
        float offsetY = Position.Y - _viewport.Height / 2f;

        var topLeft = new Vector2(offsetX, offsetY);
        var topRight = new Vector2(offsetX + _viewport.Width, offsetY);
        var bottomLeft = new Vector2(offsetX, offsetY + _viewport.Height);
        var bottomRight = new Vector2(offsetX + _viewport.Width, offsetY + _viewport.Height);

        var (tlx, tly) = IsoUtils.ScreenToWorld(topLeft);
        var (trx, try2) = IsoUtils.ScreenToWorld(topRight);
        var (blx, bly) = IsoUtils.ScreenToWorld(bottomLeft);
        var (brx, bry) = IsoUtils.ScreenToWorld(bottomRight);

        int minX = Math.Min(Math.Min(tlx, trx), Math.Min(blx, brx)) - padding;
        int minY = Math.Min(Math.Min(tly, try2), Math.Min(bly, bry)) - padding;
        int maxX = Math.Max(Math.Max(tlx, trx), Math.Max(blx, brx)) + padding;
        int maxY = Math.Max(Math.Max(tly, try2), Math.Max(bly, bry)) + padding;

        return (minX, minY, maxX, maxY);
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class BlockRenderer
{
    // Block texture: 64 wide, 48 tall (32 for top diamond + 16 for wall height)
    public static int TexWidth => GameConfig.TileWidth;
    public static int TexHeight => GameConfig.TileHeight + GameConfig.BlockVisualHeight;

    private Texture2D _blockTexture;

    public Texture2D BlockTexture => _blockTexture;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _blockTexture = CreateBlockTexture(graphicsDevice);
    }

    /// <summary>
    /// Returns the draw position for a block at the given tile and layer.
    /// Layer 0 = ground level, layer 1 = stacked on top of layer 0, etc.
    /// </summary>
    public Vector2 GetDrawPosition(int tileX, int tileY, int layer)
    {
        Vector2 screenPos = IsoUtils.WorldToScreen(tileX, tileY, layer);
        return new Vector2(
            screenPos.X - TexWidth / 2f,
            screenPos.Y - IsoUtils.BlockVisualHeight
        );
    }

    private static Texture2D CreateBlockTexture(GraphicsDevice device)
    {
        var texture = new Texture2D(device, TexWidth, TexHeight);
        var data = new Color[TexWidth * TexHeight];

        var topColor = GameConfig.BlockTopColor;
        var leftColor = GameConfig.BlockLeftColor;
        var rightColor = GameConfig.BlockRightColor;
        var borderColor = GameConfig.BlockBorderColor;

        // Isometric cube vertices:
        // Top diamond: top(32,0) right(63,16) bottom(32,32) left(0,16)
        // Drop each by wallH=16 for bottom face:
        // Left wall:  (0,16) → (32,32) → (32,48) → (0,32)
        // Right wall: (32,32) → (63,16) → (63,32) → (32,48)

        float halfW = TexWidth / 2f; // 32

        for (int py = 0; py < TexHeight; py++)
        {
            for (int px = 0; px < TexWidth; px++)
            {
                Color c = Color.Transparent;

                // --- Top face: diamond ---
                // Center at (32, 16), half-extents (32, 16)
                float dxTop = MathF.Abs(px - halfW) / halfW;
                float dyTop = MathF.Abs(py - 16f) / 16f;
                if (dxTop + dyTop <= 1f && py >= 0 && py <= 32)
                {
                    c = (dxTop + dyTop > 0.9f) ? borderColor : topColor;
                }

                // --- Left face: parallelogram ---
                // Vertices: (0,16) (32,32) (32,48) (0,32)
                // Top edge:    y = 16 + 0.5 * x     (x: 0→32, y: 16→32)
                // Bottom edge: y = 32 + 0.5 * x     (x: 0→32, y: 32→48)
                if (c == Color.Transparent && px >= 0 && px < 32)
                {
                    float topEdgeY = 16f + 0.5f * px;
                    float bottomEdgeY = 32f + 0.5f * px;
                    if (py >= topEdgeY && py <= bottomEdgeY)
                    {
                        bool isBorder = py <= topEdgeY + 1 || py >= bottomEdgeY - 1
                                     || px <= 1;
                        c = isBorder ? borderColor : leftColor;
                    }
                }

                // --- Right face: parallelogram ---
                // Vertices: (32,32) (63,16) (63,32) (32,48)
                // Top edge:    y = 48 - 0.5 * x     (x: 32→63, y: 32→16.5)
                // Bottom edge: y = 64 - 0.5 * x     (x: 32→63, y: 48→32.5)
                if (c == Color.Transparent && px >= 32 && px < 64)
                {
                    float topEdgeY = 48f - 0.5f * px;
                    float bottomEdgeY = 64f - 0.5f * px;
                    if (py >= topEdgeY && py <= bottomEdgeY)
                    {
                        bool isBorder = py <= topEdgeY + 1 || py >= bottomEdgeY - 1
                                     || px >= 62;
                        c = isBorder ? borderColor : rightColor;
                    }
                }

                data[py * TexWidth + px] = c;
            }
        }

        texture.SetData(data);
        return texture;
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public static class EntityFactory
{
    // Cached textures for door state swapping
    public static Texture2D DoorClosedTexture { get; private set; }
    public static Texture2D DoorOpenTexture { get; private set; }

    public static int CreatePlayer(EntityManager em, GraphicsDevice gd)
    {
        int id = em.CreateEntity();

        em.Positions[id] = new Position { TilePosition = GameConfig.PlayerStartTile };
        em.Sprites[id] = new Sprite
        {
            Texture = CreatePlayerTexture(gd),
            Width = GameConfig.PlayerSpriteWidth,
            Height = GameConfig.PlayerSpriteHeight,
            DrawLayer = 2
        };
        em.PlayerControlled[id] = new PlayerControlled
        {
            MoveSpeed = GameConfig.PlayerMoveSpeed,
            FacingDirection = new Vector2(1, 0)
        };

        return id;
    }

    public static int CreateDoor(EntityManager em, GraphicsDevice gd, int tileX, int tileY, bool startOpen)
    {
        // Ensure textures are created
        if (DoorClosedTexture == null)
        {
            DoorClosedTexture = CreateBlockStyleTexture(gd, GameConfig.DoorClosedColor);
            DoorOpenTexture = CreateBlockStyleTexture(gd, GameConfig.DoorOpenColor);
        }

        int id = em.CreateEntity();

        em.Positions[id] = new Position { TilePosition = new Vector2(tileX, tileY) };
        em.Sprites[id] = new Sprite
        {
            Texture = startOpen ? DoorOpenTexture : DoorClosedTexture,
            Width = GameConfig.TileWidth,
            Height = GameConfig.TileHeight + GameConfig.BlockVisualHeight,
            DrawLayer = 1,
            OffsetY = GameConfig.BlockVisualHeight
        };
        em.Collisions[id] = new Collision { BlocksMovement = !startOpen };
        em.Interactables[id] = new Interactable { Type = InteractionType.Door, IsOpen = startOpen };

        em.AddToSpatialIndex(id, tileX, tileY);
        return id;
    }

    public static int CreateFurniture(EntityManager em, GraphicsDevice gd, int tileX, int tileY)
    {
        int id = em.CreateEntity();

        em.Positions[id] = new Position { TilePosition = new Vector2(tileX, tileY) };
        em.Sprites[id] = new Sprite
        {
            Texture = CreateBlockStyleTexture(gd, GameConfig.FurnitureColor),
            Width = GameConfig.TileWidth,
            Height = GameConfig.TileHeight + GameConfig.BlockVisualHeight,
            DrawLayer = 1,
            OffsetY = GameConfig.BlockVisualHeight
        };
        em.Collisions[id] = new Collision { BlocksMovement = true };
        em.Interactables[id] = new Interactable { Type = InteractionType.Push };
        em.Pushables[id] = new Pushable();

        em.AddToSpatialIndex(id, tileX, tileY);
        return id;
    }

    public static int CreateItem(EntityManager em, GraphicsDevice gd, int tileX, int tileY)
    {
        int id = em.CreateEntity();
        int size = 10;

        em.Positions[id] = new Position { TilePosition = new Vector2(tileX, tileY) };
        em.Sprites[id] = new Sprite
        {
            Texture = CreateItemTexture(gd, size, GameConfig.ItemColor),
            Width = size,
            Height = size,
            DrawLayer = 2
        };
        em.Interactables[id] = new Interactable { Type = InteractionType.Pickup };
        em.Pickupables[id] = new Pickupable();

        em.AddToSpatialIndex(id, tileX, tileY);
        return id;
    }

    private static Texture2D CreatePlayerTexture(GraphicsDevice device)
    {
        int w = GameConfig.PlayerSpriteWidth;
        int h = GameConfig.PlayerSpriteHeight;
        var texture = new Texture2D(device, w, h);
        var data = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                data[y * w + x] = isBorder
                    ? GameConfig.PlayerBorderColor
                    : GameConfig.PlayerFillColor;
            }
        }

        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Creates a simple isometric block texture (same shape as BlockRenderer) with a given color.
    /// </summary>
    private static Texture2D CreateBlockStyleTexture(GraphicsDevice device, Color baseColor)
    {
        int texW = GameConfig.TileWidth;
        int texH = GameConfig.TileHeight + GameConfig.BlockVisualHeight;
        var texture = new Texture2D(device, texW, texH);
        var data = new Color[texW * texH];

        var topColor = baseColor;
        var leftColor = new Color(
            (int)(baseColor.R * 0.7f),
            (int)(baseColor.G * 0.7f),
            (int)(baseColor.B * 0.7f),
            baseColor.A);
        var rightColor = new Color(
            (int)(baseColor.R * 0.5f),
            (int)(baseColor.G * 0.5f),
            (int)(baseColor.B * 0.5f),
            baseColor.A);
        var borderColor = new Color(
            (int)(baseColor.R * 0.3f),
            (int)(baseColor.G * 0.3f),
            (int)(baseColor.B * 0.3f),
            baseColor.A);

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

    private static Texture2D CreateItemTexture(GraphicsDevice device, int size, Color color)
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
}

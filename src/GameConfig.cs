using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Game;

public static class GameConfig
{
    // === Display ===
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;
    public const int TargetFps = 60;

    // === Isometric Tiles ===
    public const int TileWidth = 64;
    public const int TileHeight = 32;
    public const int BlockVisualHeight = 16;

    // === Player ===
    public const float PlayerMoveSpeed = 100f;
    public const int PlayerSpriteWidth = 16;
    public const int PlayerSpriteHeight = 32;
    public static readonly Vector2 PlayerStartTile = new(100f, 100f);
    public static readonly Color PlayerBorderColor = new(150, 20, 20);
    public static readonly Color PlayerFillColor = new(220, 40, 40);

    // === Camera ===
    public const float CameraSmoothTime = 0.01f;

    // === Chunk System ===
    public const int ChunkSize = 16;
    public const long ChunkUnloadFrames = 300;

    // === Lighting ===
    public const int LightBlobWidth = 160;
    public const int LightBlobHeight = 112;
    public static readonly Color AmbientDay = new(200, 200, 210);
    public static readonly Color AmbientNight = new(30, 30, 60);

    // === Block Colors ===
    public static readonly Color BlockTopColor = new(180, 140, 100);
    public static readonly Color BlockLeftColor = new(130, 100, 70);
    public static readonly Color BlockRightColor = new(100, 75, 55);
    public static readonly Color BlockBorderColor = new(70, 50, 35);

    // === Tile Colors ===
    public static readonly Color TileLightColor = new(120, 170, 90);
    public static readonly Color TileDarkColor = new(90, 140, 60);

    // === Interaction ===
    public const Keys InteractKey = Keys.E;

    // === Entity Colors ===
    public static readonly Color DoorClosedColor = new(139, 90, 43);
    public static readonly Color DoorOpenColor = new(139, 90, 43, 80);
    public static readonly Color FurnitureColor = new(160, 120, 80);
    public static readonly Color ItemColor = new(255, 215, 0);
}

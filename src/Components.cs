using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public struct Position
{
    public Vector2 TilePosition;
}

public struct Sprite
{
    public Texture2D Texture;
    public int Width;
    public int Height;
    public int DrawLayer; // 0=ground, 1=block, 2=entity
    public float OffsetY;  // extra vertical draw offset (pixels)
}

public struct Collision
{
    public bool BlocksMovement;
}

public enum InteractionType : byte
{
    None = 0,
    Door,
    Pickup,
    Push
}

public struct Interactable
{
    public InteractionType Type;
    public bool IsOpen;
}

public struct PlayerControlled
{
    public float MoveSpeed;
    public Vector2 FacingDirection;
}

public struct Pushable { }

public struct Pickupable { }

using Microsoft.Xna.Framework;

namespace Game;

public enum ShapeType : byte
{
    Block,
    Diamond,
    Rect
}

public struct SpriteTemplate
{
    public ShapeType Shape;
    public int Width;
    public int Height;
    public int DrawLayer;
    public float OffsetY;
}

public struct ColorTemplate
{
    public Color Top;
    public Color Left;
    public Color Right;
}

public class EntityTemplate
{
    public string Name;
    public SpriteTemplate Sprite;
    public ColorTemplate Colors;
    public bool HasCollision;
    public bool BlocksMovement;
    public string InteractionType;
    public bool HasPushable;
    public bool HasPickupable;
    public bool StartOpen;
    public string OnInteractCallback;
}

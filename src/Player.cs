using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

/// <summary>
/// Thin wrapper around the player entity. Holds the entity ID
/// and provides convenience methods for camera following.
/// </summary>
public class Player
{
    public readonly int EntityId;
    private readonly EntityManager _entities;

    public Player(EntityManager entities, GraphicsDevice graphicsDevice)
    {
        _entities = entities;
        EntityId = EntityFactory.CreatePlayer(entities, graphicsDevice);
    }

    /// <summary>
    /// Returns the screen position for camera targeting.
    /// </summary>
    public Vector2 GetScreenPosition()
    {
        var pos = _entities.Positions[EntityId];
        return IsoUtils.WorldToScreen(pos.TilePosition.X, pos.TilePosition.Y);
    }
}

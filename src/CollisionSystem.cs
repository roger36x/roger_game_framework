using System;

namespace Game;

public class CollisionSystem
{
    private readonly EntityManager _entities;
    private readonly ChunkManager _chunks;

    public CollisionSystem(EntityManager entities, ChunkManager chunks)
    {
        _entities = entities;
        _chunks = chunks;
    }

    /// <summary>
    /// Returns true if the tile at (tileX, tileY) is passable.
    /// ignoreEntity: entity ID to exclude from collision (e.g., the moving entity itself).
    /// </summary>
    public bool CanMoveTo(float tileX, float tileY, int ignoreEntity = 0)
    {
        int tx = (int)MathF.Round(tileX);
        int ty = (int)MathF.Round(tileY);

        // Terrain collision
        var cell = _chunks.GetCell(tx, ty);
        if (cell.BlockHeight > 0)
            return false;

        // Entity collision
        int entityAtTile = _entities.GetEntityAtTile(tx, ty);
        if (entityAtTile != 0 && entityAtTile != ignoreEntity)
        {
            if (_entities.Collisions.TryGetValue(entityAtTile, out var col) && col.BlocksMovement)
                return false;
        }

        return true;
    }
}

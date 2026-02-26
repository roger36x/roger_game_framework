using System.Collections.Generic;

namespace Game;

public class EntityManager
{
    private int _nextId = 1;

    // Component stores
    public readonly Dictionary<int, Position> Positions = new();
    public readonly Dictionary<int, Sprite> Sprites = new();
    public readonly Dictionary<int, Collision> Collisions = new();
    public readonly Dictionary<int, Interactable> Interactables = new();
    public readonly Dictionary<int, PlayerControlled> PlayerControlled = new();
    public readonly Dictionary<int, Pushable> Pushables = new();
    public readonly Dictionary<int, Pickupable> Pickupables = new();

    // Spatial index: packed tile coords -> entity ID (one entity per tile)
    private readonly Dictionary<long, int> _spatialIndex = new();

    public int CreateEntity() => _nextId++;

    public void DestroyEntity(int id)
    {
        if (Positions.TryGetValue(id, out var pos))
        {
            long key = PackTileKey(
                (int)System.MathF.Round(pos.TilePosition.X),
                (int)System.MathF.Round(pos.TilePosition.Y));
            if (_spatialIndex.TryGetValue(key, out int existing) && existing == id)
                _spatialIndex.Remove(key);
            Positions.Remove(id);
        }
        Sprites.Remove(id);
        Collisions.Remove(id);
        Interactables.Remove(id);
        PlayerControlled.Remove(id);
        Pushables.Remove(id);
        Pickupables.Remove(id);
    }

    public void AddToSpatialIndex(int entityId, int tileX, int tileY)
    {
        long key = PackTileKey(tileX, tileY);
        _spatialIndex[key] = entityId;
    }

    public void RemoveFromSpatialIndex(int tileX, int tileY)
    {
        _spatialIndex.Remove(PackTileKey(tileX, tileY));
    }

    public void MoveSpatialIndex(int entityId, int oldTX, int oldTY, int newTX, int newTY)
    {
        long oldKey = PackTileKey(oldTX, oldTY);
        if (_spatialIndex.TryGetValue(oldKey, out int existing) && existing == entityId)
            _spatialIndex.Remove(oldKey);
        _spatialIndex[PackTileKey(newTX, newTY)] = entityId;
    }

    /// <summary>
    /// Returns the entity ID at the given tile, or 0 if none.
    /// </summary>
    public int GetEntityAtTile(int tileX, int tileY)
    {
        return _spatialIndex.TryGetValue(PackTileKey(tileX, tileY), out int id) ? id : 0;
    }

    private static long PackTileKey(int tx, int ty)
    {
        return ((long)tx << 32) | (uint)ty;
    }
}

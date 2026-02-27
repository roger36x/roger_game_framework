using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class InteractionSystem
{
    private readonly EntityManager _entities;
    private readonly CollisionSystem _collision;
    private AudioSystem _audio;
    private GameAPI _gameAPI;
    private ScriptEngine _scriptEngine;

    public InteractionSystem(EntityManager entities, CollisionSystem collision)
    {
        _entities = entities;
        _collision = collision;
    }

    public void SetAudioSystem(AudioSystem audio) => _audio = audio;
    public void SetGameAPI(GameAPI api) => _gameAPI = api;
    public void SetScriptEngine(ScriptEngine engine) => _scriptEngine = engine;

    public void TryInteract(int playerEntityId)
    {
        if (!_entities.Positions.TryGetValue(playerEntityId, out var playerPos))
            return;

        int px = (int)MathF.Round(playerPos.TilePosition.X);
        int py = (int)MathF.Round(playerPos.TilePosition.Y);

        // Find nearest interactable entity in 8 neighbors
        int bestEntity = 0;
        float bestDistSq = float.MaxValue;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int entity = _entities.GetEntityAtTile(px + dx, py + dy);
                if (entity == 0 || entity == playerEntityId) continue;
                if (!_entities.Interactables.ContainsKey(entity)) continue;

                float ddx = (px + dx) - playerPos.TilePosition.X;
                float ddy = (py + dy) - playerPos.TilePosition.Y;
                float distSq = ddx * ddx + ddy * ddy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestEntity = entity;
                }
            }
        }

        if (bestEntity == 0)
            return;

        var inter = _entities.Interactables[bestEntity];

        // Get entity tile position for spatial audio
        float etx = 0, ety = 0;
        if (_entities.Positions.TryGetValue(bestEntity, out var epos))
        {
            etx = epos.TilePosition.X;
            ety = epos.TilePosition.Y;
        }

        // Check for Lua custom callback first
        if (_gameAPI != null && _gameAPI.InteractionCallbacks.TryGetValue(bestEntity, out var callbackName))
        {
            _scriptEngine?.CallFunction(callbackName, bestEntity, playerEntityId);
            return;
        }

        switch (inter.Type)
        {
            case InteractionType.Door:
                ToggleDoor(bestEntity, inter);
                _audio?.PlaySfx(inter.IsOpen ? SfxType.DoorOpen : SfxType.DoorClose, etx, ety);
                break;
            case InteractionType.Pickup:
                _audio?.PlaySfx(SfxType.Pickup, etx, ety);
                _entities.DestroyEntity(bestEntity);
                break;
            case InteractionType.Push:
                TryPushToward(bestEntity, playerPos.TilePosition);
                _audio?.PlaySfx(SfxType.Push, etx, ety);
                break;
        }
    }

    private void ToggleDoor(int entityId, Interactable inter)
    {
        inter.IsOpen = !inter.IsOpen;
        _entities.Interactables[entityId] = inter;

        // Update collision
        if (_entities.Collisions.ContainsKey(entityId))
        {
            _entities.Collisions[entityId] = new Collision { BlocksMovement = !inter.IsOpen };
        }

        // Update sprite texture (open doors use translucent texture)
        if (_entities.Sprites.TryGetValue(entityId, out var sprite))
        {
            // Try template-based textures first
            string templateName = inter.IsOpen ? "door_open" : "door";
            var tex = TextureGenerator.GetCached(templateName);

            if (tex != null)
                sprite.Texture = tex;
            else
                sprite.Texture = inter.IsOpen ? EntityFactory.DoorOpenTexture : EntityFactory.DoorClosedTexture;

            _entities.Sprites[entityId] = sprite;
        }
    }

    /// <summary>
    /// Push entity away from the player's position.
    /// </summary>
    private void TryPushToward(int entityId, Vector2 playerTilePos)
    {
        if (!_entities.Positions.TryGetValue(entityId, out var pos))
            return;

        int oldTX = (int)MathF.Round(pos.TilePosition.X);
        int oldTY = (int)MathF.Round(pos.TilePosition.Y);

        // Push direction = entity - player (away from player)
        int dirX = oldTX - (int)MathF.Round(playerTilePos.X);
        int dirY = oldTY - (int)MathF.Round(playerTilePos.Y);

        // Clamp to single tile step
        if (dirX != 0) dirX = dirX > 0 ? 1 : -1;
        if (dirY != 0) dirY = dirY > 0 ? 1 : -1;

        int newTX = oldTX + dirX;
        int newTY = oldTY + dirY;

        // Check if destination is clear
        if (!_collision.CanMoveTo(newTX, newTY, entityId))
            return;

        // Move the entity
        pos.TilePosition = new Vector2(newTX, newTY);
        _entities.Positions[entityId] = pos;
        _entities.MoveSpatialIndex(entityId, oldTX, oldTY, newTX, newTY);
    }
}

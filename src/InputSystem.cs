using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Game;

public class InputSystem
{
    private readonly EntityManager _entities;
    private readonly CollisionSystem _collision;
    private readonly InteractionSystem _interaction;
    private readonly int _playerEntityId;
    private AudioSystem _audio;

    private KeyboardState _prevKeyboard;
    private float _footstepTimer;

    public InputSystem(EntityManager entities, CollisionSystem collision,
                       InteractionSystem interaction, int playerEntityId)
    {
        _entities = entities;
        _collision = collision;
        _interaction = interaction;
        _playerEntityId = playerEntityId;
    }

    public void SetAudioSystem(AudioSystem audio) => _audio = audio;

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!_entities.PlayerControlled.TryGetValue(_playerEntityId, out var pc))
            return;
        if (!_entities.Positions.TryGetValue(_playerEntityId, out var pos))
            return;

        // Movement
        float tileSpeed = pc.MoveSpeed / 35.8f * dt;
        Vector2 move = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W))
            move += new Vector2(-1, -1);
        if (keyboard.IsKeyDown(Keys.S))
            move += new Vector2(1, 1);
        if (keyboard.IsKeyDown(Keys.A))
            move += new Vector2(-1, 1);
        if (keyboard.IsKeyDown(Keys.D))
            move += new Vector2(1, -1);

        if (move != Vector2.Zero)
        {
            move.Normalize();

            // Update facing direction (use raw input, not normalized)
            pc.FacingDirection = move;

            Vector2 delta = move * tileSpeed;
            Vector2 newPos = pos.TilePosition + delta;

            // Axis-separated collision: try full move, then each axis independently
            if (_collision.CanMoveTo(newPos.X, newPos.Y, _playerEntityId))
            {
                pos.TilePosition = newPos;
            }
            else if (_collision.CanMoveTo(newPos.X, pos.TilePosition.Y, _playerEntityId))
            {
                pos.TilePosition = new Vector2(newPos.X, pos.TilePosition.Y);
            }
            else if (_collision.CanMoveTo(pos.TilePosition.X, newPos.Y, _playerEntityId))
            {
                pos.TilePosition = new Vector2(pos.TilePosition.X, newPos.Y);
            }
            _entities.Positions[_playerEntityId] = pos;

            // Footstep audio
            _footstepTimer += dt;
            if (_footstepTimer >= GameConfig.FootstepInterval)
            {
                _footstepTimer -= GameConfig.FootstepInterval;
                _audio?.PlayFootstep(pos.TilePosition.X, pos.TilePosition.Y);
            }
        }
        else
        {
            _footstepTimer = GameConfig.FootstepInterval; // next move starts with immediate footstep
        }

        _entities.PlayerControlled[_playerEntityId] = pc;

        // Interaction (E key, single press)
        if (keyboard.IsKeyDown(GameConfig.InteractKey) && !_prevKeyboard.IsKeyDown(GameConfig.InteractKey))
        {
            _interaction.TryInteract(_playerEntityId);
        }

        _prevKeyboard = keyboard;
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game;

public struct DrawItem : IComparable<DrawItem>
{
    public float DepthKey;
    public Texture2D Texture;
    public Vector2 Position;
    public Color Tint;

    public int CompareTo(DrawItem other) => DepthKey.CompareTo(other.DepthKey);
}

public class MainGame : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private TileMap _tileMap;
    private Camera _camera;
    private BlockRenderer _blockRenderer;
    private ChunkManager _chunkManager;
    private LightingSystem _lightingSystem;

    // ECS
    private EntityManager _entities;
    private Player _player;
    private InputSystem _inputSystem;

    // Weather
    private ParticleSystem _particleSystem;
    private WeatherSystem _weatherSystem;

    private readonly List<DrawItem> _drawItems = new(4096);

    // FPS tracking
    private int _frameCount;
    private float _fpsTimer;
    private int _currentFps;

    // Debug stats
    private int _visibleTileCount;

    public MainGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = GameConfig.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConfig.ScreenHeight;
        Window.Title = "Game Prototype";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / GameConfig.TargetFps);
    }

    protected override void Initialize()
    {
        _tileMap = new TileMap();
        _camera = new Camera(GraphicsDevice.Viewport);
        _blockRenderer = new BlockRenderer();
        _chunkManager = new ChunkManager(new ProceduralGenerator(seed: 42));
        _lightingSystem = new LightingSystem(GraphicsDevice);

        // ECS setup
        _entities = new EntityManager();
        _player = new Player(_entities, GraphicsDevice);

        var collisionSystem = new CollisionSystem(_entities, _chunkManager);
        var interactionSystem = new InteractionSystem(_entities, collisionSystem);
        _inputSystem = new InputSystem(_entities, collisionSystem, interactionSystem, _player.EntityId);

        _particleSystem = new ParticleSystem(GraphicsDevice, GameConfig.MaxParticles);
        _weatherSystem = new WeatherSystem(GraphicsDevice, _particleSystem, _lightingSystem);

        // Test entities
        EntityFactory.CreateDoor(_entities, GraphicsDevice, 97, 95, startOpen: false);
        EntityFactory.CreateFurniture(_entities, GraphicsDevice, 97, 97);
        EntityFactory.CreateItem(_entities, GraphicsDevice, 96, 96);  // inside room
        EntityFactory.CreateItem(_entities, GraphicsDevice, 101, 100); // near player spawn

        _camera.Position = _player.GetScreenPosition();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _tileMap.LoadContent(GraphicsDevice);
        _blockRenderer.LoadContent(GraphicsDevice);
        _lightingSystem.LoadContent();
        _particleSystem.LoadContent();
        _weatherSystem.LoadContent();

        // Test lights
        _lightingSystem.AddLight(new LightSource
        {
            TilePosition = new Vector2(100, 100),
            Color = new Color(255, 220, 150),
            Radius = 8,
            Intensity = 1.0f
        });
        _lightingSystem.AddLight(new LightSource
        {
            TilePosition = new Vector2(105, 98),
            Color = new Color(150, 200, 255),
            Radius = 6,
            Intensity = 0.8f
        });
        _lightingSystem.AddLight(new LightSource
        {
            TilePosition = new Vector2(96, 103),
            Color = new Color(255, 180, 100),
            Radius = 10,
            Intensity = 1.0f
        });
        // Light inside the test room (97, 97)
        _lightingSystem.AddLight(new LightSource
        {
            TilePosition = new Vector2(97, 97),
            Color = new Color(255, 200, 120),
            Radius = 5,
            Intensity = 1.0f
        });
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _inputSystem.Update(gameTime);
        _camera.Follow(_player.GetScreenPosition(), dt);
        _chunkManager.Update();

        // Day/night toggle: K=day, L=night
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.K))
            _weatherSystem.SetDayNight(false);
        if (keyboard.IsKeyDown(Keys.L))
            _weatherSystem.SetDayNight(true);

        // Weather toggle: 1=rain, 2=snow, 3=fog, 4=clear
        if (keyboard.IsKeyDown(GameConfig.WeatherRainKey))
            _weatherSystem.SetWeather(WeatherType.Rain);
        if (keyboard.IsKeyDown(GameConfig.WeatherSnowKey))
            _weatherSystem.SetWeather(WeatherType.Snow);
        if (keyboard.IsKeyDown(GameConfig.WeatherFogKey))
            _weatherSystem.SetWeather(WeatherType.Fog);
        if (keyboard.IsKeyDown(GameConfig.WeatherClearKey))
            _weatherSystem.SetWeather(WeatherType.Clear);

        // Update weather and particles
        _weatherSystem.Update(dt, _camera.Position);
        _particleSystem.Update(dt);

        // FPS counter
        _fpsTimer += dt;
        _frameCount++;
        if (_fpsTimer >= 1f)
        {
            _currentFps = _frameCount;
            _frameCount = 0;
            _fpsTimer -= 1f;
            Window.Title = $"Game Prototype - FPS: {_currentFps} | Chunks: {_chunkManager.LoadedChunkCount} | Tiles: {_visibleTileCount} | Lights: {_lightingSystem.LightCount} | Weather: {_weatherSystem.CurrentWeather} | Particles: {_weatherSystem.ParticleCount}";
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // === Pass 1: Build light map FIRST (before scene, to avoid backbuffer discard) ===
        _lightingSystem.DrawLightMap(_spriteBatch, _camera, _chunkManager);

        // === Pass 2: Draw scene to backbuffer ===
        GraphicsDevice.Clear(new Color(30, 30, 40));

        _drawItems.Clear();
        _visibleTileCount = 0;

        // Get visible tile range from camera
        var (minTX, minTY, maxTX, maxTY) = _camera.GetVisibleTileBounds(padding: 3);

        // Convert to chunk range
        int minCX = FloorDiv(minTX, Chunk.Size);
        int minCY = FloorDiv(minTY, Chunk.Size);
        int maxCX = FloorDiv(maxTX, Chunk.Size);
        int maxCY = FloorDiv(maxTY, Chunk.Size);

        // Iterate visible chunks and their tiles
        for (int cy = minCY; cy <= maxCY; cy++)
        {
            for (int cx = minCX; cx <= maxCX; cx++)
            {
                Chunk chunk = _chunkManager.GetChunk(cx, cy);

                for (int ly = 0; ly < Chunk.Size; ly++)
                {
                    int wy = chunk.WorldTileY(ly);
                    if (wy < minTY || wy > maxTY) continue;

                    for (int lx = 0; lx < Chunk.Size; lx++)
                    {
                        int wx = chunk.WorldTileX(lx);
                        if (wx < minTX || wx > maxTX) continue;

                        var cell = chunk.GetCell(lx, ly);
                        _visibleTileCount++;

                        // Ground tile (apply wetness tint when raining)
                        Vector2 groundScreenPos = IsoUtils.WorldToScreen(wx, wy);
                        Color groundTint = Color.White;
                        if (_weatherSystem.Wetness > 0f)
                            groundTint = LerpColor(Color.White, GameConfig.GroundWetTint, _weatherSystem.Wetness);

                        _drawItems.Add(new DrawItem
                        {
                            DepthKey = IsoUtils.GetDepthKey(wx, wy, 0, 0),
                            Texture = _tileMap.GetGroundTexture(cell.GroundType),
                            Position = new Vector2(
                                groundScreenPos.X - IsoUtils.TileWidth / 2f,
                                groundScreenPos.Y
                            ),
                            Tint = groundTint
                        });

                        // Blocks (each layer is a separate draw item)
                        for (int layer = 0; layer < cell.BlockHeight; layer++)
                        {
                            _drawItems.Add(new DrawItem
                            {
                                DepthKey = IsoUtils.GetDepthKey(wx, wy, layer, 1),
                                Texture = _blockRenderer.BlockTexture,
                                Position = _blockRenderer.GetDrawPosition(wx, wy, layer),
                                Tint = Color.White
                            });
                        }
                    }
                }
            }
        }

        // Collect entity draw items
        foreach (var (id, sprite) in _entities.Sprites)
        {
            if (!_entities.Positions.TryGetValue(id, out var pos)) continue;

            Vector2 screenPos = IsoUtils.WorldToScreen(pos.TilePosition.X, pos.TilePosition.Y);
            float depthKey = IsoUtils.GetDepthKey(pos.TilePosition.X, pos.TilePosition.Y, 0, sprite.DrawLayer);

            _drawItems.Add(new DrawItem
            {
                DepthKey = depthKey,
                Texture = sprite.Texture,
                Position = new Vector2(
                    screenPos.X - sprite.Width / 2f,
                    screenPos.Y - sprite.Height + IsoUtils.TileHeight / 2f + sprite.OffsetY
                ),
                Tint = Color.White
            });
        }

        // Sort by depth (back to front)
        _drawItems.Sort();

        // Draw all items
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            _camera.GetTransformMatrix()
        );

        for (int i = 0; i < _drawItems.Count; i++)
        {
            var item = _drawItems[i];
            _spriteBatch.Draw(item.Texture, item.Position, item.Tint);
        }

        _spriteBatch.End();

        // === Pass 3: Composite light map over scene ===
        _lightingSystem.ApplyLightMap(_spriteBatch);

        // === Pass 4: Weather overlay (fog + particles) ===
        _weatherSystem.Draw(_spriteBatch);

        base.Draw(gameTime);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (int)MathHelper.Lerp(a.R, b.R, t),
            (int)MathHelper.Lerp(a.G, b.G, t),
            (int)MathHelper.Lerp(a.B, b.B, t)
        );
    }

    private static int FloorDiv(int a, int b)
    {
        return (a >= 0) ? a / b : (a - b + 1) / b;
    }
}

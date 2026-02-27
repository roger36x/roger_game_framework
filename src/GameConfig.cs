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

    // === Weather ===
    public const float WeatherTransitionSeconds = 5f;
    public const int MaxParticles = 2048;

    // Rain
    public const int RainTexWidth = 2;
    public const int RainTexHeight = 8;
    public const float RainParticlesPerSecond = 600f;
    public const float RainVelocityX = -30f;
    public const float RainVelocityY = 500f;
    public static readonly Color AmbientRain = new(100, 100, 130);

    // Snow
    public const int SnowTexSize = 4;
    public const float SnowParticlesPerSecond = 200f;
    public const float SnowVelocityX = 15f;
    public const float SnowVelocityY = 40f;
    public static readonly Color AmbientSnow = new(170, 175, 190);

    // Fog
    public const float FogMaxOpacity = 0.4f;
    public static readonly Vector2 FogWindDirection = new(-1f, 0.3f); // leftward with slight downward drift
    public const float FogWindSpeed = 25f; // pixels/sec in source space
    public static readonly Color FogColor = new(140, 145, 155);
    public static readonly Color AmbientFog = new(140, 145, 155);

    // Ground wetness
    public const float WetnessGainRate = 0.15f;
    public const float WetnessDryRate = 0.05f;
    public static readonly Color GroundWetTint = new(150, 160, 200);

    // Particle spawn/cull margin (pixels beyond screen edge)
    public const float ParticleSpawnMargin = 400f;

    // Weather input keys
    public const Keys WeatherRainKey = Keys.D1;
    public const Keys WeatherSnowKey = Keys.D2;
    public const Keys WeatherFogKey = Keys.D3;
    public const Keys WeatherClearKey = Keys.D4;

    // === Audio ===
    public const float MasterVolume = 0.7f;
    public const float SfxVolume = 0.8f;
    public const float AmbientVolume = 0.1f;
    public const float FootstepInterval = 0.35f;
    public const float FootstepPitchVariation = 0.15f;
    public const float AudioMaxDistance = 12f;
    public const float AudioPanScale = 0.5f;
    public const int AudioSampleRate = 44100;
    public const Keys AudioMuteKey = Keys.M;
    public const float AmbientCrossfadeSpeed = 2f;

    // === Scripting ===
    public const Keys ScriptReloadKey = Keys.F5;
}

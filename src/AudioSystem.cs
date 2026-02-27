using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Game;

public enum SfxType : byte
{
    Footstep,
    DoorOpen,
    DoorClose,
    Pickup,
    Push
}

public class AudioSystem
{
    // Pre-generated sound effects
    private readonly SoundEffect _sfxFootstep;
    private readonly SoundEffect _sfxDoorOpen;
    private readonly SoundEffect _sfxDoorClose;
    private readonly SoundEffect _sfxPickup;
    private readonly SoundEffect _sfxPush;

    // Ambient streams
    private DynamicSoundEffectInstance _ambientWeather;
    private DynamicSoundEffectInstance _ambientDayNight;

    // State
    private Vector2 _playerTilePos;
    private bool _muted;

    // Ambient control
    private WeatherType _currentAmbientWeather = WeatherType.Clear;
    private float _ambientWeatherVolume;
    private float _ambientWeatherTarget;
    private float _weatherIntensity;

    private bool _isNight;
    private float _dayNightVolume;
    private float _dayNightTarget;

    // Reusable buffer for streaming audio
    private readonly short[] _streamBuffer = new short[4096];
    private readonly byte[] _streamBytes = new byte[4096 * 2];

    // SFX instance pool (avoid creating new instances per play)
    private const int SfxPoolSize = 8;
    private readonly SoundEffectInstance[] _sfxPool;
    private int _sfxPoolIndex;

    public bool IsMuted => _muted;

    public AudioSystem()
    {
        // Generate all sound effects procedurally
        _sfxFootstep = ProceduralAudio.CreateFootstep();
        _sfxDoorOpen = ProceduralAudio.CreateDoorOpen();
        _sfxDoorClose = ProceduralAudio.CreateDoorClose();
        _sfxPickup = ProceduralAudio.CreatePickup();
        _sfxPush = ProceduralAudio.CreatePush();

        // SFX instance pool
        _sfxPool = new SoundEffectInstance[SfxPoolSize];

        // Create ambient streams (44100 Hz Mono)
        _ambientWeather = new DynamicSoundEffectInstance(GameConfig.AudioSampleRate, AudioChannels.Mono);
        _ambientDayNight = new DynamicSoundEffectInstance(GameConfig.AudioSampleRate, AudioChannels.Mono);

        // Set master volume
        SoundEffect.MasterVolume = GameConfig.MasterVolume;
    }

    public void Update(float deltaTime, Vector2 playerTilePos)
    {
        _playerTilePos = playerTilePos;

        if (_muted) return;

        // Crossfade ambient weather volume
        _ambientWeatherVolume = MoveToward(_ambientWeatherVolume, _ambientWeatherTarget,
            deltaTime * GameConfig.AmbientCrossfadeSpeed);

        // Crossfade day/night volume
        _dayNightTarget = GameConfig.AmbientVolume * 0.5f;
        _dayNightVolume = MoveToward(_dayNightVolume, _dayNightTarget,
            deltaTime * GameConfig.AmbientCrossfadeSpeed);

        // Feed ambient weather stream
        UpdateAmbientWeather();

        // Feed day/night ambient stream
        UpdateAmbientDayNight();
    }

    private void UpdateAmbientWeather()
    {
        if (_ambientWeatherVolume < 0.001f)
        {
            if (_ambientWeather.State == SoundState.Playing)
                _ambientWeather.Stop();
            return;
        }

        _ambientWeather.Volume = _ambientWeatherVolume;

        // Submit buffers when running low (keep 3 pending)
        while (_ambientWeather.PendingBufferCount < 3)
        {
            Array.Clear(_streamBuffer);

            switch (_currentAmbientWeather)
            {
                case WeatherType.Rain:
                    ProceduralAudio.GenerateRainBuffer(_streamBuffer, _weatherIntensity);
                    break;
                case WeatherType.Snow:
                case WeatherType.Fog:
                    ProceduralAudio.GenerateWindBuffer(_streamBuffer, _weatherIntensity * 0.7f);
                    break;
                default:
                    // Clear weather: silence
                    break;
            }

            ConvertAndSubmit(_ambientWeather, _streamBuffer);
        }

        if (_ambientWeather.State != SoundState.Playing)
            _ambientWeather.Play();
    }

    private void UpdateAmbientDayNight()
    {
        if (_dayNightVolume < 0.001f)
        {
            if (_ambientDayNight.State == SoundState.Playing)
                _ambientDayNight.Stop();
            return;
        }

        _ambientDayNight.Volume = _dayNightVolume;

        while (_ambientDayNight.PendingBufferCount < 3)
        {
            Array.Clear(_streamBuffer);

            if (_isNight)
                ProceduralAudio.GenerateCricketBuffer(_streamBuffer, 0.8f);
            else
                ProceduralAudio.GenerateDayAmbientBuffer(_streamBuffer, 0.5f);

            ConvertAndSubmit(_ambientDayNight, _streamBuffer);
        }

        if (_ambientDayNight.State != SoundState.Playing)
            _ambientDayNight.Play();
    }

    private void ConvertAndSubmit(DynamicSoundEffectInstance stream, short[] samples)
    {
        // Convert short[] to byte[] (little-endian PCM16)
        Buffer.BlockCopy(samples, 0, _streamBytes, 0, samples.Length * 2);
        stream.SubmitBuffer(_streamBytes, 0, samples.Length * 2);
    }

    // --- Public API for other systems ---

    public void PlaySfx(SfxType type, float tileX, float tileY)
    {
        if (_muted) return;

        SoundEffect sfx = type switch
        {
            SfxType.Footstep => _sfxFootstep,
            SfxType.DoorOpen => _sfxDoorOpen,
            SfxType.DoorClose => _sfxDoorClose,
            SfxType.Pickup => _sfxPickup,
            SfxType.Push => _sfxPush,
            _ => null
        };
        if (sfx == null) return;

        // Spatial: distance attenuation + pan
        float dx = tileX - _playerTilePos.X;
        float dy = tileY - _playerTilePos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        float volume = Math.Clamp(1f - dist / GameConfig.AudioMaxDistance, 0f, 1f);
        volume *= GameConfig.SfxVolume;
        if (volume < 0.01f) return; // too far, skip

        // Pan based on screen-space X offset (iso: right = +X tile, left = -X tile)
        // In isometric: screenX offset ≈ (dx - dy) * TileWidth/2
        float screenDx = (dx - dy) * GameConfig.TileWidth * 0.5f;
        float pan = Math.Clamp(screenDx / (GameConfig.ScreenWidth * 0.5f) * GameConfig.AudioPanScale, -1f, 1f);

        // Use pooled instance
        var instance = GetPooledInstance(sfx);
        instance.Volume = volume;
        instance.Pan = pan;
        instance.Pitch = 0;

        if (type == SfxType.Footstep)
        {
            // Random pitch variation for footsteps
            instance.Pitch = (_rng.NextSingle() * 2f - 1f) * GameConfig.FootstepPitchVariation;
        }

        instance.Play();
    }

    public void PlayFootstep(float tileX, float tileY)
    {
        PlaySfx(SfxType.Footstep, tileX, tileY);
    }

    public void SetAmbient(WeatherType weather)
    {
        if (weather == _currentAmbientWeather) return;
        _currentAmbientWeather = weather;

        if (weather == WeatherType.Clear)
            _ambientWeatherTarget = 0f;
        else
            _ambientWeatherTarget = GameConfig.AmbientVolume;
    }

    public void UpdateWeatherIntensity(float intensity)
    {
        _weatherIntensity = intensity;
        if (_currentAmbientWeather != WeatherType.Clear)
            _ambientWeatherTarget = GameConfig.AmbientVolume * intensity;
    }

    public void SetDayNight(bool isNight)
    {
        _isNight = isNight;
    }

    public void ToggleMute()
    {
        _muted = !_muted;
        if (_muted)
        {
            SoundEffect.MasterVolume = 0f;
            if (_ambientWeather.State == SoundState.Playing)
                _ambientWeather.Stop();
            if (_ambientDayNight.State == SoundState.Playing)
                _ambientDayNight.Stop();
        }
        else
        {
            SoundEffect.MasterVolume = GameConfig.MasterVolume;
        }
    }

    // --- Internal ---

    private readonly Random _rng = new();

    private SoundEffectInstance GetPooledInstance(SoundEffect sfx)
    {
        // Find a stopped instance or recycle oldest
        for (int i = 0; i < SfxPoolSize; i++)
        {
            int idx = (_sfxPoolIndex + i) % SfxPoolSize;
            if (_sfxPool[idx] == null || _sfxPool[idx].State == SoundState.Stopped)
            {
                _sfxPool[idx]?.Dispose();
                _sfxPool[idx] = sfx.CreateInstance();
                _sfxPoolIndex = (idx + 1) % SfxPoolSize;
                return _sfxPool[idx];
            }
        }

        // All busy, recycle current slot
        _sfxPool[_sfxPoolIndex]?.Dispose();
        _sfxPool[_sfxPoolIndex] = sfx.CreateInstance();
        var inst = _sfxPool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % SfxPoolSize;
        return inst;
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta) return target;
        return current + MathF.Sign(target - current) * maxDelta;
    }
}

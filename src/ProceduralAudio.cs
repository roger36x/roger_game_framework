using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Game;

public static class ProceduralAudio
{
    private static readonly Random _rng = new(12345);

    // --- Waveform generators ---

    public static void WhiteNoise(short[] buf, float amplitude)
    {
        int amp = (int)(amplitude * short.MaxValue);
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (short)(_rng.Next(-amp, amp));
    }

    /// <summary>
    /// Pink noise via Voss-McCartney algorithm (8 octaves).
    /// </summary>
    public static void PinkNoise(short[] buf, float amplitude)
    {
        const int octaves = 8;
        var octaveValues = new float[octaves];
        for (int i = 0; i < octaves; i++)
            octaveValues[i] = (_rng.NextSingle() * 2f - 1f);

        float scale = amplitude * short.MaxValue / octaves;

        for (int i = 0; i < buf.Length; i++)
        {
            // Update one octave per sample using counter trailing zeros
            int ctz = CountTrailingZeros(i + 1);
            if (ctz < octaves)
                octaveValues[ctz] = (_rng.NextSingle() * 2f - 1f);

            float sum = 0;
            for (int o = 0; o < octaves; o++)
                sum += octaveValues[o];

            buf[i] = (short)Math.Clamp((int)(sum * scale), short.MinValue, short.MaxValue);
        }
    }

    public static void Sine(short[] buf, float freq, int sampleRate, float amplitude)
    {
        float amp = amplitude * short.MaxValue;
        float inc = 2f * MathF.PI * freq / sampleRate;
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (short)(MathF.Sin(i * inc) * amp);
    }

    public static void FrequencySweep(short[] buf, float startHz, float endHz, int sampleRate, float amplitude)
    {
        float amp = amplitude * short.MaxValue;
        float phase = 0;
        for (int i = 0; i < buf.Length; i++)
        {
            float t = (float)i / buf.Length;
            float freq = startHz + (endHz - startHz) * t;
            phase += 2f * MathF.PI * freq / sampleRate;
            buf[i] = (short)(MathF.Sin(phase) * amp);
        }
    }

    // --- Envelopes ---

    public static void ApplyExponentialDecay(short[] buf, float decayRate)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            float env = MathF.Exp(-decayRate * i / buf.Length);
            buf[i] = (short)(buf[i] * env);
        }
    }

    public static void ApplyFadeIn(short[] buf, int fadeSamples)
    {
        int n = Math.Min(fadeSamples, buf.Length);
        for (int i = 0; i < n; i++)
            buf[i] = (short)(buf[i] * ((float)i / n));
    }

    public static void ApplyFadeOut(short[] buf, int fadeSamples)
    {
        int n = Math.Min(fadeSamples, buf.Length);
        int start = buf.Length - n;
        for (int i = 0; i < n; i++)
            buf[start + i] = (short)(buf[start + i] * (1f - (float)i / n));
    }

    /// <summary>Mix src into dst (additive).</summary>
    public static void Mix(short[] dst, short[] src, float volume)
    {
        int n = Math.Min(dst.Length, src.Length);
        for (int i = 0; i < n; i++)
            dst[i] = (short)Math.Clamp(dst[i] + (int)(src[i] * volume), short.MinValue, short.MaxValue);
    }

    /// <summary>Simple one-pole low-pass filter in-place.</summary>
    public static void LowPass(short[] buf, float cutoffNormalized)
    {
        float alpha = cutoffNormalized;
        float prev = buf[0];
        for (int i = 1; i < buf.Length; i++)
        {
            prev = prev + alpha * (buf[i] - prev);
            buf[i] = (short)prev;
        }
    }

    // --- Sound effect factories ---

    public static SoundEffect CreateFootstep()
    {
        int sr = GameConfig.AudioSampleRate;
        int len = sr * 80 / 1000; // 80ms
        var buf = new short[len];
        WhiteNoise(buf, 0.6f);
        LowPass(buf, 0.3f);
        ApplyExponentialDecay(buf, 6f);
        return CreateSoundEffect(buf, sr);
    }

    public static SoundEffect CreateDoorOpen()
    {
        int sr = GameConfig.AudioSampleRate;
        int len = sr * 300 / 1000; // 300ms
        var sweep = new short[len];
        FrequencySweep(sweep, 80, 200, sr, 0.4f);
        var noise = new short[len];
        WhiteNoise(noise, 0.2f);
        LowPass(noise, 0.15f);
        Mix(sweep, noise, 0.5f);
        ApplyExponentialDecay(sweep, 3f);
        ApplyFadeIn(sweep, sr * 10 / 1000);
        return CreateSoundEffect(sweep, sr);
    }

    public static SoundEffect CreateDoorClose()
    {
        int sr = GameConfig.AudioSampleRate;
        int len = sr * 200 / 1000; // 200ms
        var sweep = new short[len];
        FrequencySweep(sweep, 180, 60, sr, 0.5f);
        var noise = new short[len];
        WhiteNoise(noise, 0.3f);
        LowPass(noise, 0.2f);
        Mix(sweep, noise, 0.4f);
        ApplyExponentialDecay(sweep, 4f);
        return CreateSoundEffect(sweep, sr);
    }

    public static SoundEffect CreatePickup()
    {
        int sr = GameConfig.AudioSampleRate;
        int len = sr * 150 / 1000; // 150ms
        var buf = new short[len];
        FrequencySweep(buf, 800, 1600, sr, 0.5f);
        ApplyExponentialDecay(buf, 4f);

        // Add a second harmonic for brightness
        var harm = new short[len];
        FrequencySweep(harm, 1200, 2400, sr, 0.25f);
        ApplyExponentialDecay(harm, 5f);
        Mix(buf, harm, 0.5f);
        return CreateSoundEffect(buf, sr);
    }

    public static SoundEffect CreatePush()
    {
        int sr = GameConfig.AudioSampleRate;
        int len = sr * 200 / 1000; // 200ms
        var buf = new short[len];
        WhiteNoise(buf, 0.5f);
        LowPass(buf, 0.1f);
        ApplyExponentialDecay(buf, 3.5f);
        ApplyFadeIn(buf, sr * 5 / 1000);
        return CreateSoundEffect(buf, sr);
    }

    // --- Streaming ambient generators ---

    private static readonly float[] _pinkOctaves = new float[8];
    private static int _pinkCounter;

    /// <summary>Fill buffer with rain ambience (pink noise + random ticks).</summary>
    public static void GenerateRainBuffer(short[] buf, float amplitude)
    {
        float scale = amplitude * short.MaxValue / 8f;
        for (int i = 0; i < buf.Length; i++)
        {
            _pinkCounter++;
            int ctz = CountTrailingZeros(_pinkCounter);
            if (ctz < 8) _pinkOctaves[ctz] = _rng.NextSingle() * 2f - 1f;

            float sum = 0;
            for (int o = 0; o < 8; o++) sum += _pinkOctaves[o];

            float sample = sum * scale;

            // Random rain tick (~1% chance per sample)
            if (_rng.Next(100) == 0)
                sample += (_rng.NextSingle() * 2f - 1f) * amplitude * short.MaxValue * 0.3f;

            buf[i] = (short)Math.Clamp((int)sample, short.MinValue, short.MaxValue);
        }
    }

    private static float _windPhase;
    private static float _windLpState;

    /// <summary>Fill buffer with wind ambience (low-pass filtered noise + amplitude modulation).</summary>
    public static void GenerateWindBuffer(short[] buf, float amplitude)
    {
        float amp = amplitude * short.MaxValue;
        float lpAlpha = 0.05f; // strong low-pass
        for (int i = 0; i < buf.Length; i++)
        {
            float noise = _rng.NextSingle() * 2f - 1f;
            _windLpState += lpAlpha * (noise - _windLpState);

            // Slow amplitude modulation (0.3 Hz)
            _windPhase += 0.3f / GameConfig.AudioSampleRate;
            float mod = 0.6f + 0.4f * MathF.Sin(_windPhase * 2f * MathF.PI);

            buf[i] = (short)Math.Clamp((int)(_windLpState * amp * mod), short.MinValue, short.MaxValue);
        }
    }

    private static float _cricketTimer;
    private static float _cricketPhase;
    private static bool _cricketActive;
    private static int _cricketPulseCount;
    private static int _cricketPulseIndex;
    private static float _cricketPulsePhase;
    private static float _cricketFreq;

    /// <summary>Fill buffer with night cricket ambience (rapid pulse chirps).</summary>
    public static void GenerateCricketBuffer(short[] buf, float amplitude)
    {
        float amp = amplitude * short.MaxValue;
        float invSr = 1f / GameConfig.AudioSampleRate;
        for (int i = 0; i < buf.Length; i++)
        {
            _cricketTimer -= invSr;
            float sample = 0;

            if (_cricketActive)
            {
                // Rapid AM pulse train: each chirp = 3-6 short pulses at ~60Hz rate
                _cricketPulsePhase += invSr;
                float pulsePeriod = 1f / 55f; // ~55 Hz pulse rate (realistic wing stroke)

                if (_cricketPulsePhase >= pulsePeriod)
                {
                    _cricketPulsePhase -= pulsePeriod;
                    _cricketPulseIndex++;
                }

                if (_cricketPulseIndex >= _cricketPulseCount)
                {
                    _cricketActive = false;
                    _cricketTimer = 0.4f + _rng.NextSingle() * 2.0f; // silence 0.4-2.4s
                }
                else
                {
                    // Each pulse: carrier shaped by a smooth hump envelope
                    float pulseT = _cricketPulsePhase / pulsePeriod;
                    // Hann-like envelope per pulse (smooth on/off, no click)
                    float pulseEnv = MathF.Sin(pulseT * MathF.PI);
                    pulseEnv *= pulseEnv; // sharper attack/release

                    // Carrier: two harmonics for richer timbre
                    _cricketPhase += _cricketFreq * invSr;
                    float carrier = MathF.Sin(_cricketPhase * 2f * MathF.PI) * 0.7f
                                  + MathF.Sin(_cricketPhase * 4f * MathF.PI) * 0.3f;

                    // Overall chirp envelope: fade out across pulses
                    float chirpEnv = 1f - (float)_cricketPulseIndex / _cricketPulseCount * 0.4f;

                    sample = carrier * pulseEnv * chirpEnv * 0.4f;
                }
            }
            else
            {
                if (_cricketTimer <= 0)
                {
                    _cricketActive = true;
                    _cricketPulseCount = 3 + _rng.Next(4); // 3-6 pulses per chirp
                    _cricketPulseIndex = 0;
                    _cricketPulsePhase = 0;
                    _cricketPhase = 0;
                    // Randomize frequency slightly per chirp (3800-4600 Hz)
                    _cricketFreq = 3800f + _rng.NextSingle() * 800f;
                }
            }

            buf[i] = (short)Math.Clamp((int)(sample * amp), short.MinValue, short.MaxValue);
        }
    }

    /// <summary>Fill buffer with very quiet day ambience (soft pink noise).</summary>
    public static void GenerateDayAmbientBuffer(short[] buf, float amplitude)
    {
        PinkNoise(buf, amplitude * 0.15f);
    }

    // --- Utility ---

    public static SoundEffect CreateSoundEffect(short[] samples, int sampleRate)
    {
        byte[] wav = PcmToWav(samples, sampleRate, 1);
        using var ms = new MemoryStream(wav);
        return SoundEffect.FromStream(ms);
    }

    public static byte[] PcmToWav(short[] samples, int sampleRate, int channels)
    {
        int dataSize = samples.Length * 2;
        using var ms = new MemoryStream(44 + dataSize);
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + dataSize);
        bw.Write(new char[] { 'W', 'A', 'V', 'E' });

        // fmt chunk
        bw.Write(new char[] { 'f', 'm', 't', ' ' });
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * 2); // byte rate
        bw.Write((short)(channels * 2)); // block align
        bw.Write((short)16); // bits per sample

        // data chunk
        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(dataSize);
        for (int i = 0; i < samples.Length; i++)
            bw.Write(samples[i]);

        return ms.ToArray();
    }

    private static int CountTrailingZeros(int v)
    {
        if (v == 0) return 32;
        int c = 0;
        while ((v & 1) == 0) { c++; v >>= 1; }
        return c;
    }
}

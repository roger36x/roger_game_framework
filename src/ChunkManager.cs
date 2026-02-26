using System;
using System.Collections.Generic;

namespace Game;

public class ChunkManager
{
    private readonly Dictionary<long, Chunk> _chunks = new();
    private readonly IChunkGenerator _generator;
    private long _currentFrame;

    private static long UnloadThresholdFrames => GameConfig.ChunkUnloadFrames;

    public int LoadedChunkCount => _chunks.Count;

    public ChunkManager(IChunkGenerator generator)
    {
        _generator = generator;
    }

    private static long PackKey(int cx, int cy)
    {
        return ((long)cx << 32) | (uint)cy;
    }

    public Chunk GetChunk(int chunkX, int chunkY)
    {
        long key = PackKey(chunkX, chunkY);
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            chunk = new Chunk(chunkX, chunkY);
            _generator.Generate(chunk);
            _chunks[key] = chunk;
        }
        chunk.LastUsedFrame = _currentFrame;
        return chunk;
    }

    public TileCell GetCell(int worldTileX, int worldTileY)
    {
        int cx = Math.DivRem(worldTileX, Chunk.Size, out int lx);
        int cy = Math.DivRem(worldTileY, Chunk.Size, out int ly);
        if (lx < 0) { lx += Chunk.Size; cx--; }
        if (ly < 0) { ly += Chunk.Size; cy--; }
        return GetChunk(cx, cy).GetCell(lx, ly);
    }

    public void Update()
    {
        _currentFrame++;

        if (_currentFrame % 60 == 0)
        {
            EvictStaleChunks();
        }
    }

    private void EvictStaleChunks()
    {
        List<long> toRemove = null;
        foreach (var kvp in _chunks)
        {
            if (_currentFrame - kvp.Value.LastUsedFrame > UnloadThresholdFrames)
            {
                toRemove ??= new List<long>();
                toRemove.Add(kvp.Key);
            }
        }
        if (toRemove != null)
        {
            foreach (var key in toRemove)
                _chunks.Remove(key);
        }
    }
}

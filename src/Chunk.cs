namespace Game;

public class Chunk
{
    public static int Size => GameConfig.ChunkSize;

    public readonly int ChunkX;
    public readonly int ChunkY;

    /// <summary>
    /// Flat array of tile cells, indexed as [y * Size + x] for cache locality.
    /// </summary>
    public readonly TileCell[] Cells;

    public long LastUsedFrame;

    public Chunk(int chunkX, int chunkY)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        Cells = new TileCell[Size * Size];
    }

    public ref TileCell GetCell(int localX, int localY)
    {
        return ref Cells[localY * Size + localX];
    }

    public int WorldTileX(int localX) => ChunkX * Size + localX;
    public int WorldTileY(int localY) => ChunkY * Size + localY;
}

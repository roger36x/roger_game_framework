namespace Game;

public class ProceduralGenerator : IChunkGenerator
{
    private readonly int _seed;

    public ProceduralGenerator(int seed = 42)
    {
        _seed = seed;
    }

    public void Generate(Chunk chunk)
    {
        for (int ly = 0; ly < Chunk.Size; ly++)
        {
            for (int lx = 0; lx < Chunk.Size; lx++)
            {
                int wx = chunk.WorldTileX(lx);
                int wy = chunk.WorldTileY(ly);

                ref TileCell cell = ref chunk.GetCell(lx, ly);

                // Checkerboard ground
                cell.GroundType = (byte)(((wx % 2 + 2) % 2 + (wy % 2 + 2) % 2) % 2);

                // Test room: walled room at (95-99, 95-99) with entrance at (97, 95)
                if (IsTestRoomWall(wx, wy))
                {
                    cell.BlockHeight = 1;
                    continue;
                }

                // Clear room interior (no random blocks inside)
                if (wx >= 96 && wx <= 98 && wy >= 96 && wy <= 98)
                {
                    cell.BlockHeight = 0;
                    continue;
                }

                // Deterministic block placement using hash (~3% of tiles)
                int hash = HashTile(wx, wy);
                if ((hash & 0xFF) < 8)
                {
                    cell.BlockHeight = 1 + ((hash >> 8) & 1); // height 1 or 2
                }
                else
                {
                    cell.BlockHeight = 0;
                }
            }
        }
    }

    private static bool IsTestRoomWall(int wx, int wy)
    {
        // Room bounds: 95-99 x 95-99
        if (wx < 95 || wx > 99 || wy < 95 || wy > 99) return false;

        // Walls are on the border of the room
        bool onBorder = wx == 95 || wx == 99 || wy == 95 || wy == 99;
        if (!onBorder) return false;

        // Entrance gap at (97, 95) - south wall opening
        if (wx == 97 && wy == 95) return false;

        return true;
    }

    private int HashTile(int x, int y)
    {
        int h = _seed;
        h ^= x * 374761393;
        h ^= y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return h & 0x7FFFFFFF;
    }
}

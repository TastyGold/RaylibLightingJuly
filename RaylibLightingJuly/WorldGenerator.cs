using SimplexNoise;

namespace RaylibLightingJuly
{
    static class WorldGenerator
    {
        private static readonly Random rand = new Random();

        public static void GenerateRandomTiles(World world)
        {
            for (int y = 0; y < world.mapHeight; y++)
            {
                for (int x = 0; x < world.mapWidth; x++)
                {
                    world.fgTiles[x, y] = rand.NextDouble() < 0.75 ? (byte)0 : (byte)1;
                }
            }
        }

        public static void GeneratePerlinTiles(World world, float population)
        {
            float[,] values = Noise.Calc2D(world.mapWidth, world.mapHeight, 0.075f / 2);

            for (int y = 0; y < world.mapHeight; y++)
            {
                for (int x = 0; x < world.mapWidth; x++)
                {
                    byte id = (byte)(values[x, y] > population * 128 ? 2 : 3);
                    world.fgTiles[x, y] = values[x, y] > population * 256 ? (byte)0 : (byte)id;
                    world.bgTiles[x, y] = 3;
                }
            }
        }

        public static void GenerateHeightmapTerrain(World world, float population)
        {
            float[] values = Noise.Calc1D(world.mapWidth, 0.075f / 2);

            for (int y = 0; y < world.mapHeight; y++)
            {
                for (int x = 0; x < world.mapWidth; x++)
                {
                    float v = values[x] / 10;
                    byte id = 0;
                    if (y - 510 > v) id = 3;
                    else if (y - 500 > v) id = 2;
                    world.fgTiles[x, y] = id;

                    id = 0;
                    if (y - 515 > v) id = 3;
                    else if (y - 505 > v) id = 2;
                    world.bgTiles[x, y] = id;
                }
            }
        }

        public static void GenerateSpiralTiles(World world)
        {
            for (int by = 0; by < world.mapHeight; by++)
            {
                for (int bx = 0; bx < world.mapWidth; bx++)
                {
                    world.bgTiles[bx, by] = 1;
                }
            }

            int x = world.mapWidth / 2;
            int y = world.mapHeight / 2;

            int[] offsetsX = { 1, 0, -1, 0 };
            int[] offsetsY = { 0, -1, 0, 1 };

            int sideLength = 1;
            int count = sideLength;
            int directionindex = 0;

            while (x > -1)
            {
                if (!(x < 0 || x >= world.mapWidth || y < 0 || y >= world.mapHeight))
                {
                    world.fgTiles[x, y] = 1;
                }

                count--;

                x += offsetsX[directionindex];
                y += offsetsY[directionindex];

                if (count == 0)
                {
                    sideLength++;
                    count = sideLength;
                    directionindex++;
                    directionindex %= 4;
                }
            }
        }

        public static void AddTorches(World world, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                world.fgTiles[rand.Next(0, world.mapWidth), rand.Next(0, world.mapHeight)] = 4;
            }
        }
    }
}
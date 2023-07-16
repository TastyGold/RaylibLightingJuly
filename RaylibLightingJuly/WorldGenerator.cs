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
            float[,] values = Noise.Calc2D(world.mapWidth, world.mapHeight, 0.075f / 3);

            for (int y = 0; y < world.mapHeight; y++)
            {
                for (int x = 0; x < world.mapWidth; x++)
                {
                    world.fgTiles[x, y] = values[x, y] > population * 256 ? (byte)0 : (byte)1;
                    world.bgTiles[x, y] = 1;
                }
            }
        }

        public static void AddTorches(World world, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                world.fgTiles[rand.Next(0, world.mapWidth), rand.Next(0, world.mapHeight)] = 2;
            }
        }
    }
}
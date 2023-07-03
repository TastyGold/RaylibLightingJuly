using System.Diagnostics;

namespace RaylibLightingJuly
{
    static class LightingManager
    {
        private static readonly float regionScreenPadding = 24;
        public static int regionWidth;
        public static int regionHeight;

        public static int worldWidth;
        public static int worldHeight;
        
        const int maxLightPropagations = 32;
        const int lightFalloffAir = 8;
        const int lightFalloffTile = 40;

        public static LitRegionData litRegionData = new LitRegionData(0, 0);
        private static LightLevel[,] tempLightmap = new LightLevel[0, 0];
        private static byte[,] tempTileIds = new byte[0, 0];

        private static LightLevel[] tileIdLightLevels = new LightLevel[256];
        private static int[] tileIdFalloffValues = new int[256];

        private static int[] neighbourOffsetX = { 1, 0, -1, 0 };
        private static int[] neighbourOffsetY = { 0, -1, 0, 1 };

        public static void Initialise()
        {
            tileIdLightLevels[2] = new LightLevel(222, 143, 255);
            regionWidth = (int)(GameManager.screenTileWidth + (2 * regionScreenPadding));
            regionHeight = (int)(GameManager.screenTileHeight + (2 * regionScreenPadding));
            litRegionData = new LitRegionData(regionWidth, regionHeight);
            tempLightmap = new LightLevel[regionWidth, regionHeight];
            tempTileIds = new byte[regionWidth, regionHeight];
            PopulateTileIdFalloff();
        }

        public static void PopulateTileIdFalloff()
        {
            for (int i = 0; i < tileIdFalloffValues.Length; i++)
            {
                tileIdFalloffValues[i] = i switch
                {
                    0 => lightFalloffAir,
                    1 => lightFalloffTile,
                    2 => lightFalloffAir,
                    _ => lightFalloffTile,
                };
            }
        }

        public static void BeginThreadedLightingCalculation()
        {
            const float interval = 0.05f;

            Stopwatch timer = new Stopwatch();

            while (Thread.CurrentThread.IsAlive)
            {
                timer.Restart();
                CalculateLighting(GameManager.world, tempLightmap);

                int timeTaken = (int)timer.ElapsedMilliseconds;
                int timeRemaining = (int)(interval * 1000) - timeTaken;

                DebugManager.SetLightingCalculationTime(timeTaken);

                if (timeRemaining > 0)
                {
                    Thread.Sleep(timeRemaining);
                }
            }
        }

        public static void CalculateLighting(World world, LightLevel[,] target)
        {
            int startX, startY, endX, endY;

            lock (litRegionData)
            {
                startX = Math.Max(0, (int)litRegionData.centerX - (regionWidth / 2));
                startY = Math.Max(0, (int)litRegionData.centerY - (regionHeight / 2));
            }

            endX = Math.Min(worldWidth, startX + regionWidth) - 1;
            endY = Math.Min(worldHeight, startY + regionHeight) - 1;

            lock (world)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        tempTileIds[x - startX, y - startY] = world.fgTiles[x, y];
                    }
                }
            }

            lock (litRegionData.falloffMap)
            {
                for (int y = 0; y <= endY - startY; y++)
                {
                    for (int x = 0; x <= endX - startX; x++)
                    {
                        byte tileId = tempTileIds[x, y];
                        target[x, y].Set(tileIdLightLevels[tileId]);
                        litRegionData.falloffMap[x, y] = tileIdFalloffValues[tileId];
                    }
                }

                int i = 0;
                bool changed = true;
                while (i < maxLightPropagations && changed == true)
                {
                    changed = PropagateLight(target, startX, startY, endX, endY, i % 2 == 1, i / 2 % 2 == 1);
                    i++;
                }

                DebugManager.RecordLightmapPropagations(i);
            }

            lock (litRegionData)
            {
                litRegionData.startX = startX;
                litRegionData.startY = startY;

                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                    {
                        litRegionData.lightmap[x, y] = target[x, y];
                    }
                }
            }
        }

        /// <returns>True if any change to the lightmap was made</returns>
        private static bool PropagateLight(LightLevel[,] target, int startX, int startY, int endX, int endY, bool reverseScanX, bool reverseScanY)
        {
            bool changed = false;
            endX -= startX;
            endY -= startY;

            for (int iy = 0; iy <= endY; iy++)
            {
                for (int ix = 0; ix <= endX; ix++)
                {
                    //cool optimisation of lightmap generation by swapping direction of for loops
                    int x = reverseScanX ? endX - ix : ix;
                    int y = reverseScanY ? endY - iy : iy;

                    if (target[x, y].CanPropagate)
                    {
                        for (int n = 0; n < neighbourOffsetX.Length; n++)
                        {
                            int nx = x + neighbourOffsetX[n];
                            int ny = y + neighbourOffsetY[n];

                            if (!(nx < 0 || nx >= regionWidth || ny < 0 || ny >= regionHeight || x + startX >= worldWidth || y + startY >= worldHeight))
                            {
                                int falloff = litRegionData.falloffMap[x, y];// world.fgTiles[x + startX, y + startY] == 0 ? lightFalloffAir : lightFalloffTile;
                                int red = target[x, y].red - falloff;
                                int green = target[x, y].green - falloff;
                                int blue = target[x, y].blue - falloff;

                                if (red > target[nx, ny].red)
                                {
                                    changed = true;
                                    target[nx, ny].red = (byte)red;
                                }
                                if (green > target[nx, ny].green)
                                {
                                    changed = true;
                                    target[nx, ny].green = (byte)green;
                                }
                                if (blue > target[nx, ny].blue)
                                {
                                    changed = true;
                                    target[nx, ny].blue = (byte)blue;
                                }
                            }
                        }
                    }
                }
            }
            return changed;
        }
    }
}
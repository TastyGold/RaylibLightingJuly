using System.Diagnostics;

namespace RaylibLightingJuly
{
    static class LightingManager
    {
        public const float lightingInterval = 0.05f;

        private static readonly float regionScreenPadding = 32;
        public static int regionWidth;
        public static int regionHeight;

        public static int worldWidth;
        public static int worldHeight;
        
        public const int maxLightPropagations = 12;
        public const int lightFalloffAir = 6;
        public const int lightFalloffTile = 40;

        public static LitRegionData litRegionData = new LitRegionData(0, 0);
        private static LightLevel[,] tempLightmap = new LightLevel[0, 0];
        private static byte[,] tempTileIds = new byte[0, 0];

        private static readonly LightLevel[] tileIdLightLevels = new LightLevel[256];
        private static readonly int[] tileIdFalloffValues = new int[256];

        private static int[,] falloffMap = new int[0, 0];

        public static void Initialise()
        {
            tileIdLightLevels[2] = new LightLevel(222, 143, 255);
            regionWidth = (int)(GameManager.screenTileWidth + (2 * regionScreenPadding));
            regionHeight = (int)(GameManager.screenTileHeight + (2 * regionScreenPadding));
            litRegionData = new LitRegionData(regionWidth, regionHeight);
            tempLightmap = new LightLevel[regionWidth, regionHeight];
            tempTileIds = new byte[regionWidth, regionHeight];
            falloffMap = new int[regionWidth, regionHeight];
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
            Stopwatch timer = new Stopwatch();

            while (Thread.CurrentThread.IsAlive)
            {
                timer.Restart();
                CalculateLighting(GameManager.world, tempLightmap);

                int timeTaken = (int)timer.ElapsedMilliseconds;
                int timeRemaining = (int)(lightingInterval * 1000) - timeTaken;

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

            //Calculate Region Position
            lock (litRegionData)
            {
                startX = Math.Max(0, (int)litRegionData.centerX - (regionWidth / 2));
                startY = Math.Max(0, (int)litRegionData.centerY - (regionHeight / 2));
            }

            endX = Math.Min(worldWidth, startX + regionWidth) - 1;
            endY = Math.Min(worldHeight, startY + regionHeight) - 1;

            //Get TileIDs
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

            //Calculate base lightmap and falloff map
            for (int y = 0; y <= endY - startY; y++)
            {
                for (int x = 0; x <= endX - startX; x++)
                {
                    byte tileId = tempTileIds[x, y];
                    target[x, y].Set(tileIdLightLevels[tileId]);
                    falloffMap[x, y] = tileIdFalloffValues[tileId];
                }
            }

            //Run lightmap propagations
            int i = 0;
            bool changed = true;
            while (i < maxLightPropagations && changed == true)
            {
                changed = PropagateLight(target, startX, startY, endX, endY, i % 2 == 1, ((i + 1) & 2) == 2);
                i++;
            }

            DebugManager.RecordLightmapPropagations(i);

            //Upload calculated lightmap data to shared region data
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

            int ox = reverseScanX ? -1 : 1;
            int oy = reverseScanY ? -1 : 1;

            for (int iy = 0; iy <= endY; iy++)
            {
                for (int ix = 0; ix <= endX; ix++)
                {
                    //cool optimisation of lightmap generation by swapping direction of for loops
                    int x = reverseScanX ? endX - ix : ix;
                    int y = reverseScanY ? endY - iy : iy;

                    if (target[x, y].CanPropagate(falloffMap[x, y]))
                    {
                        //only attempts to propagate in the scan directions of X and Y
                        changed |= PropagateLightToNeighbour(target, x, y, x + ox, y);
                        changed |= PropagateLightToNeighbour(target, x, y, x, y + oy);
                    }
                }
            }
            return changed;
        }

        /// <returns>True if any change to the lightmap was made</returns>
        private static bool PropagateLightToNeighbour(LightLevel[,] target, int x, int y, int nx, int ny)
        {
            bool changed = false;
            if (!(ny < 0 || ny >= regionHeight || nx < 0 || nx >= regionWidth))// || x + startX >= worldWidth || y + startY >= worldHeight))
            {
                int falloff = falloffMap[x, y];
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
            return changed;
        }
    }
}
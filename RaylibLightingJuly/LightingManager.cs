using System.Diagnostics;

namespace RaylibLightingJuly
{
    static class LightingManager
    {
        public const float lightingInterval = 0.05f;

        private static readonly float regionScreenPadding = 25;
        public static int regionWidth;
        public static int regionHeight;

        public static int worldWidth;
        public static int worldHeight;

        public const int minLightPropagations = 4;
        public const int maxLightPropagations = 12;
        public const int lightFalloffAir = 10;
        public const int lightFalloffTile = 32;

        public static bool interpolateLightmap = false;

        public static LitRegionData litRegionData = null!;
        private static LightLevel[,] tempLightmap = null!;
        private static byte[,] tempTileIds = null!;

        private static LightLevel[] tileIdLightLevels = null!;
        private static int[] tileIdFalloffValues = null!;

        private static int[,] falloffMap = null!;

        public static List<PointLight> pointLights = new List<PointLight>();
        public static LightLevel skylight = new LightLevel(255, 251, 213);

        public static void Initialise(float screenTileWidth, float screenTileHeight, World targetWorld)
        {
            tileIdLightLevels = new LightLevel[TileDataManager.IDs.Length];
            tileIdFalloffValues = new int[TileDataManager.IDs.Length];
            tileIdLightLevels[4] = new LightLevel(/*(purple) 222, 143, 255*/227, 191, 136);
            regionWidth = (int)(screenTileWidth + (2 * regionScreenPadding));
            regionHeight = (int)(screenTileHeight + (2 * regionScreenPadding));
            litRegionData = new LitRegionData(regionWidth, regionHeight);
            tempLightmap = new LightLevel[regionWidth, regionHeight];
            tempTileIds = new byte[regionWidth, regionHeight];
            falloffMap = new int[regionWidth, regionHeight];
            worldWidth = targetWorld.mapWidth;
            worldHeight = targetWorld.mapHeight;
            litRegionData.targetWorld = targetWorld;
            PopulateTileIdFalloff();
        }

        private static void PopulateTileIdFalloff()
        {
            for (int i = 0; i < tileIdFalloffValues.Length; i++)
            {
                tileIdFalloffValues[i] = TileDataManager.IsTileSolid(i) ? lightFalloffTile : lightFalloffAir; 
            }
        }

        public static void SetLitRegionCenter(float x, float y)
        {
            lock (litRegionData)
            {
                litRegionData.centerX = x;
                litRegionData.centerY = y;
            }
        }

        public static void StartLightingThread()
        {
            Thread lightingThread = new Thread(new ThreadStart(BeginThreadedLightingCalculation));
            lightingThread.IsBackground = true;
            lightingThread.Start();
        }

        private static void BeginThreadedLightingCalculation()
        {
            Stopwatch timer = new Stopwatch();

            while (Thread.CurrentThread.IsAlive)
            { 
                timer.Restart();
                if (litRegionData.targetWorld is not null)
                {
                    CalculateLighting(litRegionData.targetWorld, tempLightmap);
                }

                int timeTaken = (int)timer.ElapsedMilliseconds;
                int timeRemaining = (int)(lightingInterval * 1000) - timeTaken;

                DebugManager.SetLightingCalculationTime(timeTaken);

                if (timeRemaining > 0)
                {
                    Thread.Sleep(timeRemaining);
                }
            }
        }

        private static void CalculateLighting(World world, LightLevel[,] target)
        {
            int startX, startY, endX, endY, i;
            bool interpolate = interpolateLightmap;
    
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
                    if (TileDataManager.IDs[world.fgTiles[x + startX, y + startY]].transparent && world.bgTiles.IsTileEmpty(x + startX, y + startY)) target[x, y].BlendAdditive(skylight);
                    falloffMap[x, y] = tileIdFalloffValues[tileId];
                }
            }

            //Add light from point lights
            for (i = 0; i < pointLights.Count; i++)
            {
                PointLight p = pointLights[i];
                float x = p.worldPosX - startX;
                float y = p.worldPosY - startY;

                ApplyPointLight(target, x, y, p.values);
            }

            //Run lightmap propagations
            i = 0;
            bool changed = true;
            while (i < minLightPropagations || (i < maxLightPropagations && changed == true))
            {
                changed = PropagateLight(target, startX, startY, endX, endY, i % 2 == 1, ((i + 1) & 2) == 2);
                i++;
            }
            DebugManager.RecordLightmapPropagations(i);

            //Interpolate lightmap if smoothing enabled
            if (interpolate)
            {
                InterpolateLightmap(target);
            }

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

                litRegionData.interpolated = interpolate;
            }
        }

        private static void ApplyPointLight(LightLevel[,] target, float x, float y, LightLevel levels)
        {
            const float blend = 0.75f;
            const float factor = 1 / blend;

            if (x >= 0 && y >= 0 && x < regionWidth - 1 && y < regionHeight - 1)
            {
                int tileX = (int)(x - 0.5f); //gets top left tile of 2x2 mouse region
                int tileY = (int)(y - 0.5f);

                float localX = x - tileX - 0.5f; //local values will always be between 0 and 1
                float localY = y - tileY - 0.5f;

                float fx0 = localX <= (1 - blend) ? 1 : (1 - localX) * factor; //oh god oh fuck
                float fy0 = localY <= (1 - blend) ? 1 : (1 - localY) * factor;
                float fx1 = localX > blend ? 1 : localX * factor;
                float fy1 = localY > blend ? 1 : localY * factor;

                int falloff = falloffMap[(int)x, (int)y];
                int f0 = (int)(falloff * (2 - fx0 - fy0)); //math
                int f1 = (int)(falloff * (2 - fx0 - fy1));
                int f2 = (int)(falloff * (2 - fx1 - fy1));
                int f3 = (int)(falloff * (2 - fx1 - fy0));

                target[tileX, tileY] = LightLevel.Subtract(levels, f0);
                target[tileX, tileY + 1] = LightLevel.Subtract(levels, f1);
                target[tileX + 1, tileY + 1] = LightLevel.Subtract(levels, f2);
                target[tileX + 1, tileY] = LightLevel.Subtract(levels, f3);
            }
        }

        private static void InterpolateLightmap(LightLevel[,] target)
        {
            for (int y = 0; y < regionHeight - 1; y++)
            {
                for (int x = 0; x < regionWidth - 1; x++)
                {
                    target[x, y] = LightLevel.GetCornerAverage(
                        target[x, y],
                        target[x, y + 1],
                        target[x + 1, y + 1],
                        target[x + 1, y]
                        );
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

            const float sqrt2 = 1.41421356237f;

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
                        changed |= PropagateLightToNeighbour(target, x, y, x + ox, y, 1);
                        changed |= PropagateLightToNeighbour(target, x, y, x, y + oy, 1);
                        
                        //optional diagonal propagation
                        changed |= PropagateLightToNeighbour(target, x, y, x + ox, y + oy, sqrt2);
                    }
                }
            }
            return changed;
        }

        /// <returns>True if any change to the lightmap was made</returns>
        private static bool PropagateLightToNeighbour(LightLevel[,] target, int x, int y, int nx, int ny, float falloffModifier)
        {
            bool changed = false;
            if (ny >= 0 && ny < regionHeight && nx >= 0 && nx < regionWidth)// || x + startX >= worldWidth || y + startY >= worldHeight))
            {
                int falloff = (int)(falloffMap[x, y] * falloffModifier);
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
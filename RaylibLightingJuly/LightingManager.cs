namespace RaylibLightingJuly
{
    static class LightingManager
    {
        private static readonly float regionScreenPadding = 20;
        public static int regionWidth;
        public static int regionHeight;
        
        const int maxLightPropagations = 32;
        const int lightFalloffAir = 8;
        const int lightFalloffTile = 40;

        public static LightLevel[,] lightmap;
        public static int startX, startY;

        public static LightLevel[] tileIdLightLevels = new LightLevel[255];

        public static int[] neighbourOffsetX = { 1, 0, -1, 0 };
        public static int[] neighbourOffsetY = { 0, -1, 0, 1 };

        public static void Initialise()
        {
            tileIdLightLevels[2] = new LightLevel(255, 242, 204);
            regionWidth = (int)(GameManager.screenTileWidth + (2 * regionScreenPadding));
            regionHeight = (int)(GameManager.screenTileHeight + (2 * regionScreenPadding));
            lightmap = new LightLevel[regionWidth, regionHeight];
        }

        public static void CalculateLighting(World world, float cameraX, float cameraY)
        {
            startX = Math.Max(0, (int)cameraX - (regionWidth / 2));
            startY = Math.Max(0, (int)cameraY - (regionHeight / 2));

            int endX = Math.Min(world.mapWidth, startX + regionWidth) - 1;
            int endY = Math.Min(world.mapHeight, startY + regionHeight) - 1;

            for (int y = 0; y <= endY - startY; y++)
            {
                for (int x = 0; x <= endX - startX; x++)
                {
                    lightmap[x, y].Set(tileIdLightLevels[world.fgTiles[x + startX, y + startY]]);
                }
            }

            int i = 0;
            bool changed = true;
            while (i < maxLightPropagations && changed == true)
            {
                changed = PropagateLight(world, startX, startY, endX, endY, i % 2 == 1, i / 2 % 2 == 1);
                i++;
            }

            DebugManager.RecordLightmapPropagations(i);
        }

        /// <returns>True if any change to the lightmap was made</returns>
        private static bool PropagateLight(World world, int startX, int startY, int endX, int endY, bool reverseScanX, bool reverseScanY)
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

                    if (lightmap[x, y].CanPropagate)
                    {
                        for (int n = 0; n < neighbourOffsetX.Length; n++)
                        {
                            int nx = x + neighbourOffsetX[n];
                            int ny = y + neighbourOffsetY[n];

                            if (!(nx < 0 || nx >= regionWidth || ny < 0 || ny >= regionHeight || x + startX >= world.mapWidth || y + startY >= world.mapHeight))
                            {
                                int falloff = world.fgTiles[x + startX, y + startY] == 0 ? lightFalloffAir : lightFalloffTile;
                                int red = lightmap[x, y].red - falloff;
                                int green = lightmap[x, y].green - falloff;
                                int blue = lightmap[x, y].blue - falloff;

                                if (red > lightmap[nx, ny].red)
                                {
                                    changed = true;
                                    lightmap[nx, ny].red = (byte)red;
                                }
                                if (green > lightmap[nx, ny].green)
                                {
                                    changed = true;
                                    lightmap[nx, ny].green = (byte)green;
                                }
                                if (blue > lightmap[nx, ny].blue)
                                {
                                    changed = true;
                                    lightmap[nx, ny].blue = (byte)blue;
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
using Raylib_cs;

namespace RaylibLightingJuly
{
    static class WorldRenderer
    {
        private static readonly Color[] simpleTileColors = new Color[255];

        public static int pixelsPerTile = 4;

        public static void Initialise()
        {
            simpleTileColors[0] = Color.BLACK;
            simpleTileColors[1] = Color.WHITE;
            simpleTileColors[2] = new Color(245, 193, 110, 255);
        }

        public static void DrawTilesSimple(World world)
        {
            for (int y = 0; y < world.mapHeight; y++)
            {
                for (int x = 0; x < world.mapWidth; x++)
                {
                    if (!world.fgTiles.IsTileEmpty(x, y))
                    {
                        Raylib.DrawRectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, simpleTileColors[world.fgTiles[x, y]]);
                    }
                }
            }
        }

        public static void DrawTilesSimpleLit(World world, bool drawUnlitTiles)
        {
            int startX = drawUnlitTiles ? 0 : Math.Clamp(LightingManager.startX, 0, world.mapWidth - 1);
            int startY = drawUnlitTiles ? 0 : Math.Clamp(LightingManager.startY, 0, world.mapHeight - 1);
            int endX = drawUnlitTiles ? world.mapWidth : Math.Clamp(LightingManager.startX + LightingManager.regionWidth, 0, world.mapWidth - 1);
            int endY = drawUnlitTiles ? world.mapHeight : Math.Clamp(LightingManager.startY + LightingManager.regionHeight, 0, world.mapHeight - 1);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    bool bg = !world.bgTiles.IsTileEmpty(x, y);
                    bool fg = !world.fgTiles.IsTileEmpty(x, y);
                    bool lit = !(
                        x < LightingManager.startX ||
                        y < LightingManager.startY ||
                        x >= LightingManager.startX + LightingManager.regionWidth ||
                        y >= LightingManager.startY + LightingManager.regionHeight
                        );

                    if (bg && !fg)
                    {
                        Color col = Tint(simpleTileColors[world.bgTiles[x, y]], new LightLevel(128, 128, 128));
                        if (lit)
                        {
                            col = Tint(col, LightingManager.lightmap[x - LightingManager.startX, y - LightingManager.startY]);
                        }
                        Raylib.DrawRectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, col);
                    }

                    if (fg)
                    {
                        Color col = simpleTileColors[world.fgTiles[x, y]];
                        if (lit)
                        {
                            col = Tint(col, LightingManager.lightmap[x - LightingManager.startX, y - LightingManager.startY]);
                        }
                        Raylib.DrawRectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, col);
                    }
                }
            }
        }

        public static Color Tint(Color col, LightLevel tint)
        {
            return new Color(
                (byte)(col.r * ((float)tint.red / 256)),
                (byte)(col.g * ((float)tint.green / 256)),
                (byte)(col.b * ((float)tint.blue / 256)),
                (byte)(255)
                );
        }
    }
}
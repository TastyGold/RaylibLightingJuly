using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class WorldRenderer
    {
        private static readonly WorldRendererSettings settings = new();

        public const int pixelScale = 4;
        public const int tileResolution = 12;
        public const int pixelsPerTile = pixelScale * tileResolution;

        public static Texture2D[] tileAtlases = null!;

        public static byte[,] dirtBlendMask = null!;
        public static Texture2D dirtBlendingAtlas;

        public static void Initialise()
        {
            LoadTileAtlases();
            dirtBlendMask = new byte[LightingManager.regionWidth, LightingManager.regionHeight];
            dirtBlendingAtlas = Raylib.LoadTexture(FileManager.graphicsDirectory + "Tiles/blending_dirt.png");
        }

        public static void LoadTileAtlases()
        {
            tileAtlases = new Texture2D[TileDataManager.IDs.Length];
            for (int i = 0; i < tileAtlases.Length; i++)
            {
                TileDataManager.TileIdData data = TileDataManager.IDs[i];
                if (data.atlasPath is not null)
                {
                    tileAtlases[i] = Raylib.LoadTexture(FileManager.graphicsDirectory + data.atlasPath);
                }
            }
        }
        
        public static void Draw(World world)
        {
            switch(settings.renderMode)
            {
                case RenderMode.SimpleUnlit:
                    DrawTilesSimple(world);
                    break;
                case RenderMode.Simple:
                    DrawTilesSimple(world);
                    break;
                case RenderMode.Normal or _:
                    DrawLitRegionTiles(world);
                    break;
            }
        }

        public static void SetRenderMode(RenderMode r)
        {
            settings.renderMode = r;
        }
        public static RenderMode GetRenderMode() => settings.renderMode;
        public static void ToggleLighting()
        {
            settings.enableLighting = !settings.enableLighting;
        }

        public static void DrawTilesSimple(World world, bool drawUnlitTiles = false)
        {
            lock (LightingManager.litRegionData)
            {
                int startX = drawUnlitTiles ? 0 : Math.Clamp(LightingManager.litRegionData.startX, 0, world.mapWidth);
                int startY = drawUnlitTiles ? 0 : Math.Clamp(LightingManager.litRegionData.startY, 0, world.mapHeight);
                int endX = drawUnlitTiles ? world.mapWidth : Math.Clamp(LightingManager.litRegionData.startX + LightingManager.regionWidth, 0, world.mapWidth);
                int endY = drawUnlitTiles ? world.mapHeight : Math.Clamp(LightingManager.litRegionData.startY + LightingManager.regionHeight, 0, world.mapHeight);

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        bool bg = !world.bgTiles.IsTileEmpty(x, y);
                        bool fg = !world.fgTiles.IsTileEmpty(x, y);
                        bool applyLighting = settings.enableLighting && !(
                            x < LightingManager.litRegionData.startX ||
                            y < LightingManager.litRegionData.startY ||
                            x >= LightingManager.litRegionData.startX + LightingManager.regionWidth ||
                            y >= LightingManager.litRegionData.startY + LightingManager.regionHeight
                            );

                        if (bg && !fg)
                        {
                            Color col = Tint(TileDataManager.GetMapColor(world.bgTiles[x, y]), new LightLevel(128, 128, 128));
                            if (applyLighting)
                            {
                                col = Tint(col, LightingManager.litRegionData.lightmap[x - LightingManager.litRegionData.startX, y - LightingManager.litRegionData.startY]);
                            }
                            Raylib.DrawRectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, col);
                        }

                        if (fg)
                        {
                            Color col = TileDataManager.GetMapColor(world.fgTiles[x, y]);
                            if (applyLighting)
                            {
                                col = Tint(col, LightingManager.litRegionData.lightmap[x - LightingManager.litRegionData.startX, y - LightingManager.litRegionData.startY]);
                            }
                            Raylib.DrawRectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, col);
                        }
                    }
                }
            }
        }

        public static void DrawLitRegionTiles(World world)
        {
            lock (LightingManager.litRegionData)
            {
                int startX = Math.Clamp(LightingManager.litRegionData.startX, 0, world.mapWidth);
                int startY = Math.Clamp(LightingManager.litRegionData.startY, 0, world.mapHeight);
                int endX = Math.Clamp(LightingManager.litRegionData.startX + LightingManager.regionWidth, 0, world.mapWidth);
                int endY = Math.Clamp(LightingManager.litRegionData.startY + LightingManager.regionHeight, 0, world.mapHeight);

                Color bgTileColor = new Color(96, 96, 96, 255);

                //Calculate blend masks
                for (int y = startY + 1; y < endY - 1; y++)
                {
                    for (int x = startX + 1; x < endX - 1; x++)
                    {
                        int maskX = x - startX;
                        int maskY = y - startY;
                        if (TileDataManager.IDs[world.fgTiles[x, y]].isDirt)
                        {
                            dirtBlendMask[maskX, maskY - 1] |= 0b0001;
                            dirtBlendMask[maskX - 1, maskY] |= 0b0010;
                            dirtBlendMask[maskX + 1, maskY] |= 0b0100;
                            dirtBlendMask[maskX, maskY + 1] |= 0b1000;
                        }
                    }
                }

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        bool fg = !world.fgTiles.IsTileEmpty(x, y);
                        bool bg = !world.bgTiles.IsTileEmpty(x, y);

                        //Background tiles
                        if (!fg || ((world.fgTexIds[x, y] != 17 || TileDataManager.IDs[world.fgTiles[x, y]].transparent) && bg))
                        {
                            Rectangle srec = new Rectangle(12, 12, 12, 12);
                            Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                            Texture2D atlas = tileAtlases[world.bgTiles[x, y]];
                            Raylib.DrawTexturePro(atlas, srec, drec, Vector2.Zero, 0, settings.enableLighting ? Tint(bgTileColor, LightingManager.litRegionData.lightmap[x - startX, y - startY]) : bgTileColor);
                        }

                        //Foreground Tiles
                        if (fg)
                        {
                            if (world.fgTiles[x, y] == (int)TileDataManager.TileId.Torch)
                            {
                                Raylib.DrawRectangle((int)((x + 0.25f) * pixelsPerTile), (int)((y + 0.25f) * pixelsPerTile), pixelsPerTile / 2, pixelsPerTile / 2, TileDataManager.GetMapColor(4));
                            }
                            else
                            {
                                Rectangle srec = new Rectangle(AutoTilingManager.GetTilesetTileX(world.fgTexIds[x, y]) * 12, AutoTilingManager.GetTilesetTileY(world.fgTexIds[x, y]) * 12, 12, 12);
                                Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                                Texture2D atlas = tileAtlases[world.fgTiles[x, y]];
                                Raylib.DrawTexturePro(atlas, srec, drec, Vector2.Zero, 0, settings.enableLighting ? GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - startX, y - startY]) : Color.WHITE);
                            }

                            //Blending
                            if (dirtBlendMask[x - startX, y - startY] != 0 && TileDataManager.IDs[world.fgTiles[x, y]].blendDirt)
                            {
                                Rectangle srec = new Rectangle((dirtBlendMask[x - startX, y - startY] & 0b11) * 12, (dirtBlendMask[x - startX, y - startY] >> 2) * 12, 12, 12);
                                Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                                Raylib.DrawTexturePro(dirtBlendingAtlas, srec, drec, Vector2.Zero, 0, settings.enableLighting ? GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - startX, y - startY]) : Color.WHITE);
                            }
                        }
                        dirtBlendMask[x - startX, y - startY] = 0;
                    }
                }

            }
        }

        public static void DrawWorldBorderLines(World world)
        {
            Color borderColor = Color.VIOLET;
            int width = world.mapWidth * pixelsPerTile;
            int height = world.mapHeight * pixelsPerTile;

            Raylib.DrawLine(-1, -1, width + 1, -1, borderColor);
            Raylib.DrawLine(0, -1, 0, height + 1, borderColor);
            Raylib.DrawLine(0, height, width + 1, height, borderColor);
            Raylib.DrawLine(width + 1, 0, width + 1, height, borderColor);
        }

        public static Color GetColorFromLightLevel(LightLevel tint)
        {
            return new Color(tint.red, tint.green, tint.blue, (byte)255);
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

    public enum RenderMode
    {
        SimpleUnlit,
        Simple,
        Normal,
    }
}

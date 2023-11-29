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

        private static Color bgTileTint = new Color(96, 96, 96, 255);

        //Initalisation
        public static void Initialise()
        {
            LoadTileAtlases();
            WorldBlendingRenderer.Initialise();
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

        //Config
        public static void SetRenderMode(RenderMode r)
        {
            settings.renderMode = r;
        }
        public static RenderMode GetRenderMode() => settings.renderMode;
        public static void SetLightingMode(LightingMode l)
        {
            settings.lightingMode = l;
            LightingManager.interpolateLightmap = l == LightingMode.Smooth;
        }
        public static LightingMode GetLightingMode() => settings.lightingMode;
        public static void ToggleTileBlending()
        {
            settings.enableTileBlending = !settings.enableTileBlending;
        }

        //Rendering
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

        private static void DrawTilesSimple(World world, bool drawUnlitTiles = false)
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
                        bool applyLighting = settings.LightingEnabled && !(
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
        private static void DrawLitRegionTiles(World world)
        {
            lock (LightingManager.litRegionData)
            {
                int startX = Math.Clamp(LightingManager.litRegionData.startX, 0, world.mapWidth);
                int startY = Math.Clamp(LightingManager.litRegionData.startY, 0, world.mapHeight);
                int endX = Math.Clamp(LightingManager.litRegionData.startX + LightingManager.regionWidth, 0, world.mapWidth);
                int endY = Math.Clamp(LightingManager.litRegionData.startY + LightingManager.regionHeight, 0, world.mapHeight);

                WorldBlendingRenderer.CalculateDirtBlendMask(world, startX, startY, endX, endY);

                for (int y = startY + 1; y < endY - 1; y++)
                {
                    for (int x = startX + 1; x < endX - 1; x++)
                    {
                        bool fg = !world.fgTiles.IsTileEmpty(x, y);
                        bool bg = !world.bgTiles.IsTileEmpty(x, y);

                        //Draw background tile
                        if (ShouldDrawBgTile(world, x, y, fg, bg))
                        {
                            DrawTileBg(world, x, y, startX, startY, fg, bg);
                        }

                        //Draw foreground tile
                        if (fg)
                        {
                            DrawTileFg(world, x, y, startX, startY);

                            //Blending
                            WorldBlendingRenderer.DrawBlendingTile(world, x, y, startX, startY, pixelsPerTile, settings);
                        }

                        //Reset dirt blend mask
                        WorldBlendingRenderer.dirtBlendMask[x - startX, y - startY] = 0;
                    }
                }

            }
        }

        private static bool ShouldDrawBgTile(World world, int x, int y, bool fg, bool bg)
        {
            return !fg || ((world.fgTexIds[x, y] != 255 || TileDataManager.IDs[world.fgTiles[x, y]].transparent) && bg);
        }
        private static void DrawTileBg(World world, int x, int y, int startX, int startY, bool fg, bool bg)
        {
            Rectangle srec = new Rectangle(12, 12, 12, 12).FixBleedingEdge();
            Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
            Texture2D atlas = tileAtlases[world.bgTiles[x, y]];
            Raylib.DrawTexturePro(atlas, srec, drec, Vector2.Zero, 0, settings.LightingEnabled ? Tint(bgTileTint, LightingManager.litRegionData.lightmap[x - startX, y - startY]) : bgTileTint);
        }
        private static void DrawTileFg(World world, int x, int y, int startX, int startY)
        {
            if (world.fgTiles[x, y] == (int)TileDataManager.TileId.Torch)
            {
                Raylib.DrawRectangle((int)((x + 0.25f) * pixelsPerTile), (int)((y + 0.25f) * pixelsPerTile), pixelsPerTile / 2, pixelsPerTile / 2, TileDataManager.GetMapColor(4));
            }
            else
            {
                int variantId = AutoTilingManager.GetVariantIndex(world, x, y);
                Rectangle srec = new Rectangle(AutoTilingManager.GetTilesetTileX(world.fgTexIds[x, y]) * 12, (variantId * 3 + AutoTilingManager.GetTilesetTileY(world.fgTexIds[x, y])) * 12, 12, 12).FixBleedingEdge();
                Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                Texture2D atlas = tileAtlases[world.fgTiles[x, y]];
                Color4 colors = GetVertexColors(x - startX, y - startY);
                //Color col = settings.enableLighting ? GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - startX, y - startY]) : tint;
                RaylibExtensions.DrawTextureProInterpolated(atlas, srec, drec, Vector2.Zero, colors.c0, colors.c1, colors.c2, colors.c3);
                
            }
        }

        public static void DrawWorldBorderLines(World world)
        {
            Color borderColor = Color.VIOLET;
            int width = world.mapWidth * pixelsPerTile;
            int height = world.mapHeight * pixelsPerTile;

            RaylibExtensions.DrawRectangleLines(0, 0, width, height, 1, borderColor);
        }
        public static void DrawLitRegionBoundary(World world)
        {
            int startX = LightingManager.litRegionData.startX * pixelsPerTile;
            int startY = LightingManager.litRegionData.startY * pixelsPerTile;
            int endX = (LightingManager.litRegionData.startX + LightingManager.regionWidth) * pixelsPerTile;
            int endY = (LightingManager.litRegionData.startY + LightingManager.regionHeight) * pixelsPerTile;

            RaylibExtensions.DrawRectangleLines(startX, startY, endX, endY, 0, Color.DARKBLUE);
        }
        public static void DrawTileTexIds(World world)
        {
            int startX = Math.Clamp(LightingManager.litRegionData.startX, 0, world.mapWidth);
            int startY = Math.Clamp(LightingManager.litRegionData.startY, 0, world.mapHeight);
            int endX = Math.Clamp(LightingManager.litRegionData.startX + LightingManager.regionWidth, 0, world.mapWidth);
            int endY = Math.Clamp(LightingManager.litRegionData.startY + LightingManager.regionHeight, 0, world.mapHeight);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Raylib.DrawText(world.fgTexIds[x, y].ToString(), pixelsPerTile * x + (pixelsPerTile / 2 - pixelsPerTile / 8), pixelsPerTile * y + (pixelsPerTile / 2 - pixelsPerTile / 8), pixelsPerTile / 4, Color.BLACK);
                }
            }
        }

        //Utility
        public static Color4 GetVertexColors(int x, int y)
        {
            if (settings.lightingMode == LightingMode.Unlit)
            {
                return new Color4(Color.WHITE);
            }
            else if (LightingManager.litRegionData.interpolated)
            {
                Color c0 = GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - 1, y - 1]);
                Color c1 = GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - 1, y]);
                Color c2 = GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x, y]);
                Color c3 = GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x, y - 1]);
                return new Color4(c0, c1, c2, c3);
            }
            else return new Color4(GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x, y]));
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

    public enum LightingMode
    {
        Unlit,
        Retro,
        Smooth,
    }
}

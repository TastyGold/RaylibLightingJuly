using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class WorldBlendingRenderer
    {
        public static byte[,] dirtBlendMask = null!;
        public static Texture2D dirtBlendingAtlas;

        private static readonly byte[] grassBlendMasks = { 0b11101100, 0b11011010, 0b10110101, 0b01110011 };
        private static readonly int[] NoX = { 0, -1, 1, 0 }; //neighbour offsets
        private static readonly int[] NoY = { -1, 0, 0, 1 };

        public static void Initialise()
        {
            dirtBlendMask = new byte[LightingManager.regionWidth, LightingManager.regionHeight];
            dirtBlendingAtlas = Raylib.LoadTexture(FileManager.graphicsDirectory + "Tiles/blending_dirt.png");
        }

        public static void CalculateDirtBlendMask(World world, int startX, int startY, int endX, int endY)
        {
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
                    else if (TileDataManager.IDs[world.fgTiles[x, y]].isGrass)
                    {
                        int mask = GetGrassBlendMask(world.fgTexIds[x, y]);
                        dirtBlendMask[maskX, maskY - 1] |= (byte)(0b0001 & mask);
                        dirtBlendMask[maskX - 1, maskY] |= (byte)(0b0010 & mask);
                        dirtBlendMask[maskX + 1, maskY] |= (byte)(0b0100 & mask);
                        dirtBlendMask[maskX, maskY + 1] |= (byte)(0b1000 & mask);
                    }
                }
            }
        }

        public static int GetGrassBlendMask(byte texId)
        {
            int output = 0;
            for (int i = 0; i < 4; i++)
            {
                output |= (texId & grassBlendMasks[i]) == grassBlendMasks[i] ? 1 << i : 0;
            }
            return output;
        }

        public static void DrawBlendingTile(World world, int x, int y, int startX, int startY, int pixelsPerTile, WorldRendererSettings settings)
        {
            if (dirtBlendMask[x - startX, y - startY] != 0 && TileDataManager.IDs[world.fgTiles[x, y]].blendDirt && settings.enableTileBlending)
            {
                Rectangle srec = new Rectangle((dirtBlendMask[x - startX, y - startY] & 0b11) * 12, (dirtBlendMask[x - startX, y - startY] >> 2) * 12, 12, 12).FixBleedingEdge();
                Rectangle drec = new Rectangle(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                Color4 colors = WorldRenderer.GetVertexColors(x - startX, y - startY);
                //Color col = settings.enableLighting ? WorldRenderer.GetColorFromLightLevel(LightingManager.litRegionData.lightmap[x - startX, y - startY]) : Color.WHITE;
                RaylibExtensions.DrawTextureProInterpolated(dirtBlendingAtlas, srec, drec, Vector2.Zero, colors.c0, colors.c1, colors.c2, colors.c3);
            }
        }
    }
}

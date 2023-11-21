namespace RaylibLightingJuly
{
    static class WorldTileManager
    {
        public static void ReplaceSurroundedGrass(World world, int startX, int startY, int endX, int endY)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (TileDataManager.IDs[world.fgTiles[x, y]].isGrass && world.fgTexIds[x, y] == 255) world.fgTiles.SetTile(x, y, (int)TileDataManager.TileId.Dirt);
                }
            }
        }
    }
}

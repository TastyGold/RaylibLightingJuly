namespace RaylibLightingJuly
{
    public class World
    {
        public readonly int mapWidth;
        public readonly int mapHeight;

        public TilingLayer<byte> fgTiles;
        public TilingLayer<byte> bgTiles;

        public World(int width, int height)
        {
            mapWidth = width;
            mapHeight = height;

            fgTiles = new TilingLayer<byte>(mapWidth, mapHeight, byte.MinValue);
            bgTiles = new TilingLayer<byte>(mapWidth, mapHeight, byte.MinValue);
        }
    }
}
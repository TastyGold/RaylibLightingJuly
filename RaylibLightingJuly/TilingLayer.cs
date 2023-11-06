namespace RaylibLightingJuly
{
    internal class TilingLayer<T>
    {
        public readonly int width;
        public readonly int height;
        public readonly T emptyId;

        private readonly T[,] map;

        public void InitialiseTilemap()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[x, y] = emptyId;
                }
            }
        }

        public void SetTile(int x, int y, T id)
        {
            map[x, y] = id;
        }

        public T GetTile(int x, int y)
        {
            return map[x, y];
        }

        public bool IsTileEmpty(int x, int y)
        {
            return map[x, y]!.Equals(emptyId);
        }

        public T this[int x, int y]
        {
            get { return map[x, y]; }
            set { map[x, y] = value; }
        }

        public TilingLayer(int width, int height, T emptyId)
        {
            this.width = width;
            this.height = height;
            this.emptyId = emptyId;
            map = new T[width, height];
        }

    }
}
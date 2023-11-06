using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    internal class GameCamera
    {
        private Camera2D _cam;
        public Camera2D Cam => _cam;

        private float posX;
        private float posY;
        private float worldPosX;
        private float worldPosY;

        public int pixelsPerTile;

        public float Zoom
        {
            get
            {
                return _cam.zoom;
            }
            set
            {
                _cam.zoom = value;
            }
        }

        public Vector2 Target
        {
            get
            {
                return new Vector2(worldPosX, worldPosY);
            }
            set
            {
                posX = value.X * pixelsPerTile;
                posY = value.Y * pixelsPerTile;
                worldPosX = posX / pixelsPerTile;
                worldPosY = posY / pixelsPerTile;
                _cam.target = value * pixelsPerTile;
            }
        }

        public void Initialise(int screenWidth, int screenHeight, float screenTileHeight, int pixelsPerTile)
        {
            this.pixelsPerTile = pixelsPerTile;
            Zoom = screenHeight / (pixelsPerTile * screenTileHeight);
            _cam.offset = new Vector2(screenWidth / 2, screenHeight / 2);
        }
    }
}
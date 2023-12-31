﻿namespace RaylibLightingJuly
{
    internal class LitRegionData
    {
        //For thread safety reasons, all region data is stored in one object that can be locked

        public World? targetWorld;

        public LightLevel[,] lightmap;
        public int startX, startY;

        public float centerX, centerY;

        public bool interpolated;

        public LitRegionData(int width, int height)
        {
            lightmap = new LightLevel[width, height];
        }
    }
}
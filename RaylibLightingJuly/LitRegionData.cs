namespace RaylibLightingJuly
{
    public class LitRegionData
    {
        //For thread safety reasons, all region data is stored in one object that can be locked
        
        public LightLevel[,] lightmap;
        public int[,] falloffMap;
        public int startX, startY;

        public float centerX, centerY;

        public LitRegionData(int width, int height)
        {
            lightmap = new LightLevel[width, height];
            falloffMap = new int[width, height];
        }
    }
}
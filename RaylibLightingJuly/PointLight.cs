namespace RaylibLightingJuly
{
    public class PointLight
    {
        public float worldPosX;
        public float worldPosY;

        public LightLevel values;

        public PointLight()
        {
            worldPosX = 0;
            worldPosY = 0;
            values = new LightLevel(255, 255, 255);
        }

        public PointLight(float x, float y, LightLevel levels)
        {
            worldPosX = x;
            worldPosY = y;
            values = levels;
        }
    }
}
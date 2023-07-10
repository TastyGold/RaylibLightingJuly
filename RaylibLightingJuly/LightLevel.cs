namespace RaylibLightingJuly
{
    public struct LightLevel
    {
        public byte red;
        public byte green;
        public byte blue;
        public byte Magnitude => Math.Max(red, Math.Max(green, blue));

        public bool CanPropagate(int falloff)
        {
            return red > falloff || green > falloff || blue > falloff;
        }

        public void Set(byte red, byte green, byte blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        public void Set(int red, int green, int blue)
        {
            this.red = (byte)red;
            this.green = (byte)green;
            this.blue = (byte)blue;
        }

        public void Set(LightLevel l)
        {
            red = l.red;
            green = l.green;
            blue = l.blue;
        }

        public LightLevel()
        {
            red = 0;
            green = 0;
            blue = 0;
        }

        public LightLevel(byte red, byte green, byte blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        public LightLevel(int red, int green, int blue)
        {
            this.red = (byte)red;
            this.green = (byte)green;
            this.blue = (byte)blue;
        }
    }
}
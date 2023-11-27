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

        public void BlendAdditive(LightLevel l)
        {
            this = new LightLevel(l.red > red ? l.red : red, l.green > green ? l.green : green, l.blue > blue ? l.blue : blue);
        }
        public void BlendSubtractive(LightLevel l)
        {
            this = new LightLevel(l.red < red ? l.red : red, l.green < green ? l.green : green, l.blue < blue ? l.blue : blue);
        }

        public static LightLevel Subtract(LightLevel l, int n)
        {
            return new LightLevel(l.red - n, l.green - n, l.blue - n);
        }

        public static LightLevel Multiply(LightLevel l, float v)
        {
            return new LightLevel(
                (int)(l.red * v),
                (int)(l.green * v),
                (int)(l.blue * v)
                );
        }

        public static LightLevel GetCornerAverage(LightLevel l0, LightLevel l1, LightLevel l2, LightLevel l3)
        {
            int r = (l0.red + l1.red + l2.red + l3.red) >> 2;
            int g = (l0.green + l1.green + l2.green + l3.green) >> 2;
            int b = (l0.blue + l1.blue + l2.blue + l3.blue) >> 2;

            return new LightLevel(r, g, b);
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
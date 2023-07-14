using Raylib_cs;

namespace RaylibLightingJuly
{
    static class DebugManager
    {
        //Average Light Propagations
        private static int[] averageLightPropagations = new int[1];
        private static int nextLightPropagationIndex = 0;

        public static void RecordLightmapPropagations(int iterations)
        {
            averageLightPropagations[nextLightPropagationIndex] = iterations;
            nextLightPropagationIndex++;
            if (nextLightPropagationIndex == averageLightPropagations.Length)
            {
                nextLightPropagationIndex = 0;
            }
        }

        public static float GetAverageLightmapPropagations()
        {
            int sum = 0;
            for (int i = 0; i < averageLightPropagations.Length; i++)
            {
                sum += averageLightPropagations[i];
            }
            return (float)sum / averageLightPropagations.Length;
        }

        //Lighting Calculation Time
        private static int lightingCalculationMilliseconds = 0;
        private static int[] lightingCalculationMillisecondsRecord = new int[50];
        private static int nextLCMRIndex = 0;

        public static void SetLightingCalculationTime(int milliseconds)
        {
            lightingCalculationMilliseconds = milliseconds;
            lightingCalculationMillisecondsRecord[nextLCMRIndex] = milliseconds;
            nextLCMRIndex++;
            if (nextLCMRIndex >= lightingCalculationMillisecondsRecord.Length)
            {
                nextLCMRIndex -= lightingCalculationMillisecondsRecord.Length;
            }
        }

        public static int GetLightingCalculationTime()
        {
            return lightingCalculationMilliseconds;
        }

        public static float GetAverageLightingCalculationTime()
        {
            int sum = 0;
            for (int i = 0; i < lightingCalculationMillisecondsRecord.Length; i++)
            {
                sum += lightingCalculationMillisecondsRecord[i];
            }
            float average = (float)sum / lightingCalculationMillisecondsRecord.Length;
            return average;
        }

        //Frame Time Graph
        private static float[] frameTimeRecord = new float[500];
        private static int nextFrameTimeIndex = 0;
        private static readonly int[] fpsMarkers = { 1, 15, 30, 60, 120 };
        private static readonly Color[] fpsColors =
        {
            Color.RED,
            Color.ORANGE,
            Color.YELLOW,
            Color.GREEN,
            Color.GREEN
        };

        public static void RecordFrameTime(float frameTime)
        {
            frameTimeRecord[nextFrameTimeIndex] = frameTime;
            nextFrameTimeIndex++;
            if (nextFrameTimeIndex == frameTimeRecord.Length)
            {
                nextFrameTimeIndex = 0;
            }
        }

        public static void DrawFrameTimeGraph(float heightScale)
        {
            int height = Raylib.GetScreenHeight();

            for (int i = 0; i < fpsMarkers.Length; i++)
            {
                int y = (int)(height - (heightScale / fpsMarkers[i]));
                Raylib.DrawLine(0, y, frameTimeRecord.Length, y, Color.LIGHTGRAY);
                Raylib.DrawText(fpsMarkers[i] + " fps", 505, y - 4, 10, Color.LIGHTGRAY);
            }

            for (int i = 0; i < 500; i++)
            {
                int fpsColorIndex = fpsMarkers.Length - 1;
                while (1 / frameTimeRecord[i] < fpsMarkers[fpsColorIndex] && fpsColorIndex > 0)
                {
                    fpsColorIndex--;
                }

                float y = height - (frameTimeRecord[i] * heightScale);
                Raylib.DrawLine(i, (int)y, i, height, fpsColors[fpsColorIndex]);
            }
        }
    }
}
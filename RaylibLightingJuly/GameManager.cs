using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class GameManager
    {
        //Game Data
        public static World world = new World(0, 0);

        private static float mouseX, mouseY;
        public static float screenTileWidth = 120;
        public static float screenTileHeight = 67.5f;

        //private static float lightingUpdateDuration = 0.1f;
        //private static float lightingUpdateTimer = 0f;

        public static Camera2D mainCamera = new Camera2D()
        {
            offset = new Vector2(screenTileWidth * WorldRenderer.pixelsPerTile * 0.5f, screenTileHeight * WorldRenderer.pixelsPerTile * 0.5f),
            rotation = 0,
            target = Vector2.Zero,
            zoom = 1,
        };
        public static RenderTexture2D previewTexture;

        public static void Run()
        {
            Begin();

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            End();
        }

        public static void Begin()
        {
            world = new World(320, 180);
            Raylib.InitWindow(world.mapWidth * WorldRenderer.pixelsPerTile * 2, world.mapHeight * WorldRenderer.pixelsPerTile, "Tile Lighting");
            WorldRenderer.Initialise();
            WorldGenerator.GeneratePerlinTiles(world, 0.38f);
            WorldGenerator.AddTorches(world, 50);
            LightingManager.Initialise();
            LightingManager.worldWidth = world.mapWidth;
            LightingManager.worldHeight = world.mapHeight;
            previewTexture = Raylib.LoadRenderTexture((int)(screenTileWidth * WorldRenderer.pixelsPerTile), (int)(screenTileHeight * WorldRenderer.pixelsPerTile));

            Thread lightingThread = new Thread(new ThreadStart(LightingManager.BeginThreadedLightingCalculation));
            lightingThread.IsBackground = true;
            lightingThread.Start();
        }

        public static void Update()
        {
            mouseX = (float)Raylib.GetMouseX() / WorldRenderer.pixelsPerTile;
            mouseY = (float)Raylib.GetMouseY() / WorldRenderer.pixelsPerTile;

            lock (LightingManager.litRegionData)
            {
                LightingManager.litRegionData.centerX = mouseX;
                LightingManager.litRegionData.centerY = mouseY;
            }

            mainCamera.target = new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY());

            if (!(mouseX < 0 || mouseY < 0 || mouseX >= world.mapWidth || mouseY >= world.mapHeight))
            {
                if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    world.fgTiles[(int)mouseX, (int)mouseY] = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? (byte)2 : (byte)1;
                    //world.fgTiles[(int)mouseX + 1, (int)mouseY] = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? (byte)2 : (byte)1;
                    //world.fgTiles[(int)mouseX, (int)mouseY + 1] = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? (byte)2 : (byte)1;
                    //world.fgTiles[(int)mouseX - 1, (int)mouseY] = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? (byte)2 : (byte)1;
                    //world.fgTiles[(int)mouseX, (int)mouseY - 1] = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? (byte)2 : (byte)1;
                }
                if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
                {
                    world.fgTiles[(int)mouseX, (int)mouseY] = 0;
                }
            }

            float deltaTime = Raylib.GetFrameTime();
            DebugManager.RecordFrameTime(deltaTime);
        }

        public static void Draw()
        {
            Raylib.BeginDrawing();

            Raylib.BeginTextureMode(previewTexture);
            Raylib.BeginMode2D(mainCamera);
            Raylib.ClearBackground(Color.BLACK);
            WorldRenderer.DrawTilesSimpleLit(world, false);
            Raylib.EndMode2D();
            Raylib.EndTextureMode();

            Raylib.ClearBackground(Color.BLACK);
            WorldRenderer.DrawTilesSimpleLit(world, false);
            Raylib.DrawRectangleLines(
                (int)((mouseX - (screenTileWidth / 2)) * WorldRenderer.pixelsPerTile),
                (int)((mouseY - (screenTileHeight / 2)) * WorldRenderer.pixelsPerTile),
                (int)(screenTileWidth * WorldRenderer.pixelsPerTile),
                (int)(screenTileHeight * WorldRenderer.pixelsPerTile),
                Color.RED
                );
            lock (LightingManager.litRegionData)
            {
                Raylib.DrawRectangleLines(
                    LightingManager.litRegionData.startX * WorldRenderer.pixelsPerTile,
                    LightingManager.litRegionData.startY * WorldRenderer.pixelsPerTile,
                    LightingManager.regionWidth * WorldRenderer.pixelsPerTile,
                    LightingManager.regionHeight * WorldRenderer.pixelsPerTile,
                    Color.GREEN
                    );
            }
            Raylib.DrawTexturePro(previewTexture.texture,
                new Rectangle(0, 0, previewTexture.texture.width, -previewTexture.texture.height),
                new Rectangle(Raylib.GetScreenWidth() / 2, 0, Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight()),
                Vector2.Zero, 0, Color.WHITE);
            Raylib.DrawFPS(10, 10);
            Raylib.DrawText($"AvgLightProp: {DebugManager.GetAverageLightmapPropagations()}", 10, 40, 20, Color.DARKGREEN);
            Raylib.DrawText($"LightingCalc (ms): {DebugManager.GetLightingCalculationTime()}", 10, 70, 20, Color.DARKGREEN);
            Raylib.DrawText($"AvgLightingCalc (ms): {DebugManager.GetAverageLightingCalculationTime()}", 10, 100, 20, Color.DARKGREEN);
            DebugManager.DrawFrameTimeGraph(5000);

            Raylib.EndDrawing();
        }

        public static void End()
        {
            Raylib.CloseWindow();
        }
    }

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
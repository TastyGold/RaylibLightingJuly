using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class OldGameManager
    {
        //Game Data
        public static World world = new World(0, 0);

        private static float mouseX, mouseY;
        public static float screenTileWidth = 120;
        public static float screenTileHeight = 67.5f;

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
            previewTexture = Raylib.LoadRenderTexture((int)(screenTileWidth * WorldRenderer.pixelsPerTile), (int)(screenTileHeight * WorldRenderer.pixelsPerTile));
            LightingManager.Initialise(screenTileWidth, screenTileHeight, world);
            LightingManager.StartLightingThread();
            WorldRenderer.SetRenderMode(RenderMode.Simple);
        }

        public static void Update()
        {
            //Handle Camera Movement
            mouseX = (float)Raylib.GetMouseX() / WorldRenderer.pixelsPerTile;
            mouseY = (float)Raylib.GetMouseY() / WorldRenderer.pixelsPerTile;

            lock (LightingManager.litRegionData)
            {
                LightingManager.litRegionData.centerX = mouseX;
                LightingManager.litRegionData.centerY = mouseY;
            }

            mainCamera.target = new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY());

            //Handle Tile Editing
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

            //Record Frame Time
            float deltaTime = Raylib.GetFrameTime();
            DebugManager.RecordFrameTime(deltaTime);
        }

        public static void Draw()
        {
            Raylib.BeginDrawing();

            Raylib.BeginTextureMode(previewTexture);
            Raylib.BeginMode2D(mainCamera);
            Raylib.ClearBackground(Color.BLACK);
            WorldRenderer.Draw(world);
            WorldRenderer.DrawWorldBorderLines(world);
            Raylib.EndMode2D();
            Raylib.EndTextureMode();

            Raylib.ClearBackground(Color.BLACK);
            //WorldRenderer.DrawTilesSimpleLit(world, true);
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
}
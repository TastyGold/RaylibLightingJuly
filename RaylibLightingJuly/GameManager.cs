using Raylib_cs;
using System.Numerics;
using System.Diagnostics;

namespace RaylibLightingJuly
{
    static class GameManager
    {
        public static World? world;
        public static GameCamera mainCamera = new GameCamera();
        public static float mainCameraSpeed = 50;

        public static PointLight mouseLight = new PointLight();
        public static float mouseLightHue = 0;
        public static float mouseLightHueCycleRate = 180;

        public static float screenTileWidth = 120;
        public static float screenTileHeight = 62.5f;

        public static int screenWidth = 1600;
        public static int screenHeight = 900;

        public static bool drawTiles = true;
        public static int selectedTileId = 1;

        public static Stopwatch debugStopwatch = new Stopwatch();

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
            Raylib.InitWindow(screenWidth, screenHeight, "RaylibLightingJuly");

            TileDataManager.Initialise();

            world = new World(1000, 1000);
            //WorldGenerator.GeneratePerlinTiles(world, 0.38f);
            WorldGenerator.GenerateHeightmapTerrain(world, 0.5f);
            //WorldGenerator.AddTorches(world, 1);

            AutoTilingManager.Initialise();
            AutoTilingManager.UpdateTileIndexes(world, 0, 0, world.mapWidth - 1, world.mapHeight - 1);

            mainCamera.Initialise(screenWidth, screenHeight, screenTileHeight, WorldRenderer.pixelsPerTile);
            mainCamera.Target = new Vector2(world.mapWidth / 2, world.mapHeight / 2);

            LightingManager.Initialise(screenTileWidth, screenTileHeight, world);
            LightingManager.StartLightingThread();

            WorldRenderer.Initialise();
            WorldRenderer.SetRenderMode(RenderMode.Normal);
            WorldRenderer.SetLightingMode(LightingMode.Smooth);

            LightingManager.pointLights.Add(mouseLight);
        }

        public static void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            DebugManager.RecordFrameTime(deltaTime);

            Vector2 movementInput = GetMovementInput();
            HandleCameraMovement(movementInput, deltaTime);

            Vector2 mouseWorldPosition = GetMouseWorldPosition();
            HandleCameraZoom();
            HandleTilePainting(mouseWorldPosition);
            HandleMousePointLight(mouseWorldPosition, deltaTime, false);

            if (selectedTileId < 0) selectedTileId = TileDataManager.IDs.Length - 1;
            if (selectedTileId >= TileDataManager.IDs.Length - 1) selectedTileId = 0;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_T)) drawTiles = !drawTiles;
            if ((Raylib.IsKeyPressed(KeyboardKey.KEY_R) && !Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))) WorldRenderer.SetRenderMode(WorldRenderer.GetRenderMode() == RenderMode.Simple ? RenderMode.Normal : RenderMode.Simple);
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_L) || (Raylib.IsKeyPressed(KeyboardKey.KEY_R) && Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))) WorldRenderer.SetLightingMode((LightingMode)(((int)WorldRenderer.GetLightingMode() + 1) % 3));
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT)) selectedTileId++;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT)) selectedTileId--;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_B)) WorldRenderer.ToggleTileBlending();
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE)) selectedTileId = 1;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO)) selectedTileId = 2;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_THREE)) selectedTileId = 3;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_G) && world is not null) WorldTileManager.ReplaceSurroundedGrass(world, 0, 0, world.mapWidth - 1, world.mapHeight - 1);
    
            LightingManager.SetLitRegionCenter(mainCamera.Target.X, mainCamera.Target.Y);
        }

        public static void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(194, 251, 255, 255));
            Raylib.BeginMode2D(mainCamera.Cam);
            if (world is not null)
            {
                debugStopwatch.Start();
                WorldRenderer.Draw(world);
                WorldRenderer.DrawWorldBorderLines(world);
                WorldRenderer.DrawLitRegionBoundary(world);
                //WorldRenderer.DrawTileTexIds(world);
                DebugManager.RecordRenderMilliseconds((int)debugStopwatch.ElapsedMilliseconds);
                debugStopwatch.Reset();
            }
            DrawCameraBounds();
            Raylib.EndMode2D();
            DrawDebugOverlay();
            Raylib.EndDrawing();
        }

        public static void End()
        {
            Raylib.CloseWindow();
        }

        public static Vector2 GetMovementInput()
        {
            float x = 0;
            float y = 0;

            if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) x--;
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) x++;
            if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) y--;
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) y++;

            return new Vector2(x, y);
        }
        public static Vector2 GetMouseWorldPosition()
        {
            return Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), mainCamera.Cam) / WorldRenderer.pixelsPerTile;
        }

        public static void HandleCameraMovement(Vector2 movementInput, float deltaTime)
        {
            if (movementInput != Vector2.Zero)
            {
                float shiftBoost = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) ? 5 : 1;
                mainCamera.Target += mainCameraSpeed * deltaTime * shiftBoost * movementInput;
            }
        }

        public static void HandleTilePainting(Vector2 mousePos)
        {
            int mouseX = (int)mousePos.X;
            int mouseY = (int)mousePos.Y;

            if (!(mouseX < 0 || mouseX >= world!.mapWidth || mouseY < 0 || mouseY >= world!.mapHeight))
            {
                bool changed = false;
                if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
                {
                    if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                    {
                        world!.fgTiles[mouseX, mouseY] = 4;
                        changed = true;
                    }
                }
                else if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    world!.fgTiles[mouseX, mouseY] = (byte)selectedTileId;
                    changed = true;
                }

                if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
                {
                    world!.fgTiles[mouseX, mouseY] = 0;
                    changed = true;
                }
                if (changed)
                {
                    AutoTilingManager.UpdateTileIndexes(world!, mouseX - 1, mouseY - 1, mouseX + 1, mouseY + 1);
                }
            }
        }

        public static void HandleMousePointLight(Vector2 mouseWorldPos, float deltaTime, bool rainbow)
        {
            mouseLight.worldPosX = mouseWorldPos.X;
            mouseLight.worldPosY = mouseWorldPos.Y;

            mouseLightHue += deltaTime * mouseLightHueCycleRate;
            if (mouseLightHue > 360) mouseLightHue -= 360;

            Color col = rainbow ? Raylib.ColorFromHSV(mouseLightHue, 1, 1) : Color.WHITE;

            mouseLight.values.Set((byte)(col.r * 0.3f + 175f), (byte)(col.g * 0.3f + 175f), (byte)(col.b * 0.3f + 175f));
        }

        public static void HandleCameraZoom()
        {
            if (Raylib.GetMouseWheelMove() != 0)
            {
                mainCamera.Zoom *= Raylib.GetMouseWheelMove() > 0 ? 1.25f : 0.8f;
            }
        }

        public static void DrawCameraBounds()
        {
            Color borderColor = Color.PINK;
            int w = (int)(screenTileWidth * WorldRenderer.pixelsPerTile / 2) + 1;
            int h = (int)(screenTileHeight * WorldRenderer.pixelsPerTile / 2) + 1;
            int x = (int)(mainCamera.Target.X * WorldRenderer.pixelsPerTile);
            int y = (int)(mainCamera.Target.Y * WorldRenderer.pixelsPerTile);

            Raylib.DrawLine(x - w, y - h, x + w, y - h, borderColor);
            Raylib.DrawLine(x - w, y + h, x + w, y + h, borderColor);
            Raylib.DrawLine(x - w, y - h, x - w, y + h, borderColor);
            Raylib.DrawLine(x + w, y - h, x + w, y + h, borderColor);
        }

        public static void DrawDebugOverlay()
        {
            Raylib.DrawFPS(10, 10);
            Raylib.DrawText($"AvgLightProp: {DebugManager.GetAverageLightmapPropagations()}", 10, 40, 20, Color.DARKGREEN);
            Raylib.DrawText($"LightingCalc (ms): {DebugManager.GetLightingCalculationTime()}", 10, 70, 20, Color.DARKGREEN);
            Raylib.DrawText($"AvgLightingCalc (ms): {DebugManager.GetAverageLightingCalculationTime()}", 10, 100, 20, Color.DARKGREEN);
            Raylib.DrawText($"MousePosition: <{(int)GetMouseWorldPosition().X}, {(int)GetMouseWorldPosition().Y}>", 10, 130, 20, Color.DARKGREEN);
            Raylib.DrawText($"PaintTileID: <{selectedTileId}>", 10, 160, 20, Color.DARKGREEN);
            Raylib.DrawText($"RenderTime (ms): {DebugManager.GetRenderMilliseconds()}", 10, 190, 20, Color.DARKGREEN);
            Raylib.DrawText($"CameraZoom: {mainCamera.Zoom}", 10, 220, 20, Color.DARKGREEN);
            Raylib.DrawText($"LightingMode: {WorldRenderer.GetLightingMode()}", 10, 250, 20, Color.DARKGREEN);
            DebugManager.DrawFrameTimeGraph(5000);
        }
    }
}
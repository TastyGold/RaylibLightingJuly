using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class GameManager
    {
        //Game Data
        private static World world;

        private static float mouseX, mouseY;
        public static float screenTileWidth = 120;
        public static float screenTileHeight = 67.5f;

        private static float lightingUpdateDuration = 0.1f;
        private static float lightingUpdateTimer = 0f;

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

            while(!Raylib.WindowShouldClose())
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
            WorldGenerator.AddTorches(world, 100);
            LightingManager.Initialise();
            previewTexture = Raylib.LoadRenderTexture((int)(screenTileWidth * WorldRenderer.pixelsPerTile), (int)(screenTileHeight * WorldRenderer.pixelsPerTile));
        }

        public static void Update()
        {
            mouseX = (float)Raylib.GetMouseX() / WorldRenderer.pixelsPerTile;
            mouseY = (float)Raylib.GetMouseY() / WorldRenderer.pixelsPerTile;

            mainCamera.target = new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY());

            lightingUpdateTimer -= Raylib.GetFrameTime();
            if (lightingUpdateTimer <= 0)
            {
                lightingUpdateTimer += lightingUpdateDuration;
                LightingManager.CalculateLighting(world, mouseX, mouseY);
            }
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
            Raylib.DrawRectangleLines(
                (int)(LightingManager.startX * WorldRenderer.pixelsPerTile),
                (int)(LightingManager.startY * WorldRenderer.pixelsPerTile),
                (int)(LightingManager.regionWidth * WorldRenderer.pixelsPerTile),
                (int)(LightingManager.regionHeight * WorldRenderer.pixelsPerTile),
                Color.GREEN
                );
            Raylib.DrawTexturePro(previewTexture.texture,
                new Rectangle(0, 0, previewTexture.texture.width, -previewTexture.texture.height),
                new Rectangle(Raylib.GetScreenWidth() / 2, 0, Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight()),
                Vector2.Zero, 0, Color.WHITE);
            Raylib.DrawFPS(10, 10);

            Raylib.EndDrawing();
        }

        public static void End()
        {
            Raylib.CloseWindow();
        }
    }
}
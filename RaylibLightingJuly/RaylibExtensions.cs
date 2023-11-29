using Raylib_cs;
using System.Numerics;

namespace RaylibLightingJuly
{
    static class RaylibExtensions
    {
        private const int RL_QUADS = 7;

        // Draw a part of a texture (defined by a rectangle) with 'pro' parameters
        // NOTE: origin is relative to destination rectangle size
        public static void DrawTextureProInterpolated(Texture2D texture, Rectangle source, Rectangle dest, Vector2 origin, Color4 colors)
        {
            DrawTextureProInterpolated(texture, source, dest, origin, colors.c0, colors.c1, colors.c2, colors.c3);
        }
        public static void DrawTextureProInterpolated(Texture2D texture, Rectangle source, Rectangle dest, Vector2 origin, Color c0, Color c1, Color c2, Color c3)
        {
            // Check if texture is valid
            if (texture.id > 0)
            {
                float width = (float)texture.width;
                float height = (float)texture.height;

                bool flipX = false;

                if (source.width < 0) { flipX = true; source.width *= -1; }
                if (source.height < 0) source.y -= source.height;

                Vector2 topLeft;
                Vector2 topRight;
                Vector2 bottomLeft;
                Vector2 bottomRight;

                float x = dest.x - origin.X;
                float y = dest.y - origin.Y;
                topLeft = new Vector2(x, y);
                topRight = new Vector2(x + dest.width, y);
                bottomLeft = new Vector2(x, y + dest.height);
                bottomRight = new Vector2(x + dest.width, y + dest.height);

                Rlgl.rlSetTexture(texture.id);
                Rlgl.rlBegin(RL_QUADS);

                Rlgl.rlNormal3f(0.0f, 0.0f, 1.0f);                          // Normal vector pointing towards viewer

                // Top-left corner for texture and quad
                Rlgl.rlColor4ub(c0.r, c0.g, c0.b, c0.a);
                if (flipX) Rlgl.rlTexCoord2f((source.x + source.width) / width, source.y / height);
                else Rlgl.rlTexCoord2f(source.x / width, source.y / height);
                Rlgl.rlVertex2f(topLeft.X, topLeft.Y);

                // Bottom-left corner for texture and quad
                Rlgl.rlColor4ub(c1.r, c1.g, c1.b, c1.a);
                if (flipX) Rlgl.rlTexCoord2f((source.x + source.width) / width, (source.y + source.height) / height);
                else Rlgl.rlTexCoord2f(source.x / width, (source.y + source.height) / height);
                Rlgl.rlVertex2f(bottomLeft.X, bottomLeft.Y);

                // Bottom-right corner for texture and quad
                Rlgl.rlColor4ub(c2.r, c2.g, c2.b, c2.a);
                if (flipX) Rlgl.rlTexCoord2f(source.x / width, (source.y + source.height) / height);
                else Rlgl.rlTexCoord2f((source.x + source.width) / width, (source.y + source.height) / height);
                Rlgl.rlVertex2f(bottomRight.X, bottomRight.Y);

                // Top-right corner for texture and quad
                Rlgl.rlColor4ub(c3.r, c3.g, c3.b, c3.a);
                if (flipX) Rlgl.rlTexCoord2f(source.x / width, source.y / height);
                else Rlgl.rlTexCoord2f((source.x + source.width) / width, source.y / height);
                Rlgl.rlVertex2f(topRight.X, topRight.Y);

                Rlgl.rlEnd();
                Rlgl.rlSetTexture(0);
            }
        }

        public static void DrawRectangleLines(int startX, int startY, int endX, int endY, int padding, Color color)
        {
            startX -= padding;
            startY -= padding;
            endX += padding;
            endY += padding;

            Raylib.DrawLine(startX, startY, startX, endY, color);
            Raylib.DrawLine(endX, startY, endX, endY, color);
            Raylib.DrawLine(startX, startY, endX, startY, color);
            Raylib.DrawLine(startX, endY, endX, endY, color);
        }

        private const float trim = 0.0002f;
        public static Rectangle FixBleedingEdge(this Rectangle r)
        {
            return new Rectangle(r.x + trim, r.y + trim, r.width - trim * 2, r.height - trim * 2);
        }
    }

    public struct Color4
    {
        public Color c0, c1, c2, c3;

        public Color4 (Color col)
        {
            c0 = col;
            c1 = col;
            c2 = col;
            c3 = col;
        }
        public Color4 (Color c0, Color c1, Color c2, Color c3)
        {
            this.c0 = c0;
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }
    }
}

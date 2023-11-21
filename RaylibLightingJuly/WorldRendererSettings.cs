namespace RaylibLightingJuly
{
    internal class WorldRendererSettings
    {
        public RenderMode renderMode = RenderMode.Normal;
        public LightingMode lightingMode = LightingMode.Smooth;
        public bool LightingEnabled => lightingMode != LightingMode.Unlit;
        public bool enableTileBlending = true;
    }
}

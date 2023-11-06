using Raylib_cs;
using System.Text.Json;
using Newtonsoft.Json;

namespace RaylibLightingJuly
{
    static class TileDataManager
    {
        public struct TileIdData
        {
            public int id;
            public string name;
            public Color mapColor;
            public bool solid;
            public bool transparent;
            public bool connected;
            public bool isDirt;
            public bool blendDirt;

            public string? atlasPath;
            public TileAtlasType atlasType;
            public int numVariants;

            public TileIdData()
            {
                id = -1;
                name = "unknown";
                mapColor = Color.BLACK;
                solid = true;
                transparent = false;
                connected = true;
                isDirt = false;
                blendDirt = false;
                atlasPath = "Tiles/grass.png";
                atlasType = TileAtlasType.TilesetFull16x3;
                numVariants = 4;
            }

            public override string? ToString()
            {
                return $"{{id={id}, name={name}, mapColor=({mapColor.r},{mapColor.g},{mapColor.b}), solid={solid}, transparent={transparent}, connected={connected}}}";
            }
        }

        public static TileIdData[] IDs = null!;

        public static void Initialise()
        {
            LoadTileData(FileManager.contentDirectory + "Tiles/tileData.json");
        }

        public static Color GetMapColor(int id)
        {
            return IDs[id].mapColor;
        }
        public static bool IsTileSolid(int id)
        {
            return IDs[id].solid;
        }

        public static void LoadTileData(string filename)
        {
            string json = File.ReadAllText(filename);
            var data = JsonConvert.DeserializeObject<TileIdData[]>(json)!;
            IDs = data;
            for (int i = 0; i < IDs.Length; i++)
            {
                Console.WriteLine(IDs[i]);
                IDs[i].mapColor = new Color(IDs[i].mapColor.r, IDs[i].mapColor.g, IDs[i].mapColor.b, (byte)255);
            }
        }

        public enum TileId
        {
            Air,
            Dirt,
            Grass,
            Stone,
            Torch
        }

        public enum TileAtlasType
        {
            Null,
            TilesetFull16x3,
            TilesetSimple4x4,
        }

        /*public static void WriteDataList(string filename, IEnumerable<TileIdData> dataList)
        {
            var filestream = File.OpenWrite(filename);
            var writer = new Utf8JsonWriter(filestream);
            writer.WriteStartArray();
            foreach (var dataItem in dataList)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", dataItem.id);
                writer.WriteString("name", dataItem.name);
                writer.WriteStartObject("mapColor");
                writer.WriteNumber("r", dataItem.mapColor.r);
                writer.WriteNumber("g", dataItem.mapColor.g);
                writer.WriteNumber("b", dataItem.mapColor.b);
                writer.WriteEndObject();
                writer.WriteBoolean("solid", dataItem.solid);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.Flush();
            filestream.Close();
        }*/
    }
}

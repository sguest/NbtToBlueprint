using System.Collections.Generic;

namespace NbtToBlueprint.StructureData
{
    public class StructureDataRaw
    {
        public List<int> Size { get; set; }
        public int DataVersion { get; set; }
        public List<StructureDataRawPalette> Palette { get; set; }
        public List<StructureDataRawBlock> Blocks { get; set; }
    }

    public class StructureDataRawPalette
    {
        public string Name { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    public class StructureDataRawBlock
    {
        public int State { get; set; }
        public List<int> Pos { get; set; }
        public Dictionary<string, object> Nbt { get; set; }
    }
}

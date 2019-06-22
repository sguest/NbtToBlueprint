using System.Collections.Generic;

namespace NbtToBlueprint.Blueprints
{
    public class BlockData
    {
        public IList<BlockDataItem> Blocks { get; set; } = new List<BlockDataItem>();
    }

    public class BlockDataItem
    {
        public string Name { get; set; }
        public bool Ignore { get; set; }
        public bool HideBlueprint { get; set; }
        public IList<BlockDataVariant> Variants { get; set; } = new List<BlockDataVariant>();
    }

    public class BlockDataVariant
    {
        public string BlockName { get; set; }
        public string SpriteName { get; set; }
        public int? MaterialCount { get; set; }
        public IDictionary<string, string> Props { get; set; } = new Dictionary<string, string>();
    }
}

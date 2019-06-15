using System.Collections.Generic;

namespace NbtToBlueprint.Nbt
{
    public class NbtListTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.List;
        public IList<NbtTag> ChildTags { get; set; } = new List<NbtTag>();
    }
}

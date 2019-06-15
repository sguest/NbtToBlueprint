using System.Collections.Generic;

namespace NbtToBlueprint.Nbt
{
    class NbtCompoundTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Compound;
        public IList<NbtTag> ChildTags { get; set; } = new List<NbtTag>();
    }
}

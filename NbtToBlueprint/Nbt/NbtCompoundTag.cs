﻿using System.Collections.Generic;

namespace NbtToBlueprint.Nbt
{
    public class NbtCompoundTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Compound;
        public IDictionary<string, NbtTag> ChildTags { get; set; } = new Dictionary<string, NbtTag>();
    }
}

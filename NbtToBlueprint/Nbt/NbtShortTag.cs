﻿namespace NbtToBlueprint.Nbt
{
    public class NbtShortTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Short;
        public short Payload { get; set; }
    }
}

﻿namespace NbtToBlueprint.Nbt
{
    class NbtByteTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Byte;
        public sbyte Payload { get; set; }
    }
}

﻿namespace NbtToBlueprint.Nbt
{
    public class NbtStringTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.String;
        public string Payload { get; set; }
    }
}

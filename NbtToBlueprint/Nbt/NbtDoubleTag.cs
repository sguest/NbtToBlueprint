namespace NbtToBlueprint.Nbt
{
    class NbtDoubleTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Long;
        public double Payload { get; set; }
    }
}

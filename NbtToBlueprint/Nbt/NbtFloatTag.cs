namespace NbtToBlueprint.Nbt
{
    class NbtFloatTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.Float;
        public float Payload { get; set; }
    }
}

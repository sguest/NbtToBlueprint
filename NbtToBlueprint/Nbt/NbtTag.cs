namespace NbtToBlueprint.Nbt
{
    public abstract class NbtTag
    {
        public string Name { get; set; }
        public abstract NbtTagType TagType { get; }
    }
}

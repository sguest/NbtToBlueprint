namespace NbtToBlueprint.Nbt
{
    public class NbtIntArrayTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.IntArray;
        public int[] Payload { get; set; }
    }
}

namespace NbtToBlueprint.Nbt
{
    public class NbtByteArrayTag : NbtTag
    {
        public override NbtTagType TagType => NbtTagType.ByteArray;
        public byte[] Payload { get; set; }
    }
}

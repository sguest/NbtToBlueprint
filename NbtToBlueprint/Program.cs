using NbtToBlueprint.Nbt;
using NbtToBlueprint.StructureData;

namespace NbtToBlueprint
{
    class Program
    {
        static void Main(string[] args)
        {
            var inFile = args[0];
            var outFile = args[1];

            var deserializer = new NbtDeserializer();

            using(var inputStream = System.IO.File.OpenRead(inFile)) {
                var raw = deserializer.DeserializeObject<StructureDataRaw>(inputStream);
            }
        }
    }
}

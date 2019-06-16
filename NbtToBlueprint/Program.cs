using NbtToBlueprint.Blueprints;
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
            var name = args[2];

            var deserializer = new NbtDeserializer();
            var generator = new BlueprintGenerator();
            StructureDataRaw structureData;

            using (var inputStream = System.IO.File.OpenRead(inFile)) {
                structureData = deserializer.DeserializeObject<StructureDataRaw>(inputStream);
            }

            var blueprint = generator.GenerateBlueprint(structureData, name);

            System.IO.File.WriteAllText(outFile, blueprint);
        }
    }
}

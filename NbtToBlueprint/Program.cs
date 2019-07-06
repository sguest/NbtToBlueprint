using NbtLib;
using NbtToBlueprint.Blueprints;
using NbtToBlueprint.StructureData;

namespace NbtToBlueprint
{
    class Program
    {
        static void Main(string[] args)
        {
            var inFile = args[0];
            var outFile = args[1];
            var name = System.Text.RegularExpressions.Regex.Replace(System.IO.Path.GetFileNameWithoutExtension(inFile), "(^|_)([a-z0-9])",
                    s => {
                        var result = "";
                        if (s.Groups[1].Value == "_")
                        {
                            result += " ";
                        }
                        result += s.Groups[2].Value.ToUpperInvariant();
                        return result;
                    });

            var generator = new BlueprintGenerator();
            StructureDataRaw structureData;

            using (var inputStream = System.IO.File.OpenRead(inFile)) {
                structureData = NbtConvert.DeserializeObject<StructureDataRaw>(inputStream);
            }

            var blueprint = generator.GenerateBlueprint(structureData, name);

            System.IO.File.WriteAllText(outFile, blueprint);
        }
    }
}

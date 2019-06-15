namespace NbtToBlueprint
{
    class Program
    {
        static void Main(string[] args)
        {
            var inFile = args[0];
            var outFile = args[1];

            var parser = new Nbt.NbtParser();

            using(var inputStream = System.IO.File.OpenRead(inFile)) {
                var decoded = parser.ParseFileData(inputStream);
            }
        }
    }
}

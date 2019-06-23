namespace NbtToBlueprint.Blueprints
{
    class PaletteItem
    {
        public string BlockName { get; set; }
        public string SpriteName { get; set; }
        public char BlueprintValue { get; set; }
        public int MaterialCount { get; set; }
        public bool IsDuplicate { get; set; }
    }
}

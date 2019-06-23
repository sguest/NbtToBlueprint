using NbtToBlueprint.StructureData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NbtToBlueprint.Blueprints
{
    public class BlueprintGenerator
    {
        public BlueprintGenerator()
        {
            var blockDataString = System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Config\BlockData.json"));
            this.BlockData = JsonConvert.DeserializeObject<BlockData>(blockDataString);
        }

        private BlockData BlockData;

        private List<PaletteItem> Palette;

        public string GenerateBlueprint(StructureDataRaw data, string name)
        {
            BuildPalette(data);

            var xSize = data.Size[0];
            var ySize = data.Size[1];
            var zSize = data.Size[2];

            var layers = new char[xSize, ySize,zSize];
            var emptyLayers = new bool[ySize];

            var itemCounts = new SortedDictionary<string, int[]>();

            foreach (var block in data.Blocks)
            {
                var paletteItem = Palette[block.State];
                var blockName = paletteItem.BlockName;
                if(blockName == "Jigsaw")
                {
                    paletteItem = TransformJigsaw(block);
                }
                layers[block.Pos[0], block.Pos[1], block.Pos[2]] = paletteItem.BlueprintValue;

                if(paletteItem.MaterialCount > 0)
                {
                    if (!itemCounts.ContainsKey(paletteItem.BlockName))
                    {
                        itemCounts.Add(paletteItem.BlockName, new int[ySize]);
                    }

                    itemCounts[paletteItem.BlockName][block.Pos[1]] += paletteItem.MaterialCount;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine(WriteBlueprint(xSize, ySize, zSize, name, layers, emptyLayers));
            builder.AppendLine();
            builder.AppendLine(WriteMaterialTable(ySize, emptyLayers, itemCounts));

            return builder.ToString();
        }

        private string WriteBlueprint(int xSize, int ySize, int zSize, string name, char[,,] layers, bool[] emptyLayers)
        {
            var blueprint = new StringBuilder();

            blueprint.AppendLine("{{layered blueprint|name=").Append(name).AppendLine("|default=Layer 1");

            foreach (var item in Palette)
            {
                if (item.BlueprintValue != default(char) && !item.IsDuplicate)
                {
                    blueprint.AppendLine($"|{item.BlueprintValue}={item.SpriteName}");
                }
            }
            blueprint.AppendLine();

            var lastLayer = "";
            var lastLayerStart = 0;
            var lastLayerEnd = 0;

            for (var y = 0; y < ySize; y++)
            {
                var currentLayer = new StringBuilder();
                for (var x = xSize - 1; x >= 0; x--)
                {
                    for (var z = 0; z < zSize; z++)
                    {
                        var value = layers[x, y, z];
                        if (value == default(char))
                        {
                            currentLayer.Append(' ');
                        }
                        else
                        {
                            currentLayer.Append(value);
                        }
                    }
                    currentLayer.AppendLine();
                }
                currentLayer.AppendLine();

                var currentLayerString = currentLayer.ToString();
                if (string.IsNullOrWhiteSpace(currentLayerString))
                {
                    emptyLayers[y] = true;
                }
                else
                {
                    if (currentLayerString == lastLayer)
                    {
                        lastLayerEnd = y + 1;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(lastLayer))
                        {
                            WriteLayer(blueprint, lastLayer, lastLayerStart, lastLayerEnd);
                        }
                        lastLayer = currentLayerString;
                        lastLayerStart = y + 1;
                        lastLayerEnd = y + 1;
                    }
                }
            }
            WriteLayer(blueprint, lastLayer, lastLayerStart, lastLayerEnd);

            blueprint.AppendLine();
            blueprint.AppendLine("}}");

            return blueprint.ToString();
        }

        private string WriteMaterialTable(int numLayers, bool[] emptyLayers, IDictionary<string, int[]> itemCounts)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{| class=\"wikitable sortable mw-collapsible\"");
            builder.AppendLine("|-");
            builder.Append("!Name");
            for (var y = 0; y < numLayers; y++)
            {
                if (!emptyLayers[y])
                {
                    builder.Append($" !!Layer {y + 1}");
                }
            }
            builder.AppendLine(" !!Total");

            foreach (var item in itemCounts)
            {
                builder.AppendLine("|-");
                builder.Append("| {{BlockSprite|").Append(item.Key).Append("|link=").Append(item.Key).Append("|text=").Append(item.Key).Append("}}     ");

                for (var y = 0; y < numLayers; y++)
                {
                    if (!emptyLayers[y])
                    {
                        builder.Append("|| ");
                        if (item.Value[y] == 0)
                        {
                            builder.Append("-");
                        }
                        else
                        {
                            builder.Append(item.Value[y]);
                        }
                        builder.Append(" ");
                    }
                }
                builder.Append("|| ").AppendLine(item.Value.Sum().ToString());
            }

            builder.AppendLine("|}");
            return builder.ToString();
        }

        private PaletteItem TransformJigsaw(StructureDataRawBlock block)
        {
            var transformData = block.Nbt["final_state"].ToString().Split('[');
            var paletteData = new StructureDataRawPalette() { Name = transformData[0], Properties = new Dictionary<string, string>() };
            if (transformData.Length > 1)
            {
                var nbtData = transformData[1].TrimEnd(']');

                foreach (var dataItem in nbtData.Split(','))
                {
                    var dataParts = dataItem.Split('=');
                    paletteData.Properties.Add(dataParts[0], dataParts[1]);
                }
            }

            var paletteItem = GetPaletteItem(paletteData);
            var matchingItem = Palette.Find(m => m.SpriteName == paletteItem.SpriteName);
            if (matchingItem == null)
            {
                Palette.Add(paletteItem);
            }
            else
            {
                paletteItem = matchingItem;
            }

            return paletteItem;
        }

        private void WriteLayer(StringBuilder builder, string layerContent, int start, int end)
        {
            var layerNumString = start.ToString();
            if(start != end)
            {
                layerNumString += "-" + end;
            }
            builder.AppendLine($"|----Layer {layerNumString}|");
            builder.AppendLine();
            builder.Append(layerContent);
        }

        private void BuildPalette(StructureDataRaw data)
        {
            Palette = new List<PaletteItem>();

            foreach (var item in data.Palette)
            {
                var paletteItem = GetPaletteItem(item);

                var matchingItem = Palette.Find(m => m.SpriteName == paletteItem.SpriteName);
                if(matchingItem != null)
                {
                    paletteItem.IsDuplicate = true;
                    paletteItem.BlueprintValue = matchingItem.BlueprintValue;
                }

                Palette.Add(paletteItem);
            }
        }

        private PaletteItem GetPaletteItem(StructureDataRawPalette item)
        {
            var name = CleanSpriteName(item.Name);
            var dataItem = BlockData.Blocks.FirstOrDefault(i => Regex.IsMatch(name, i.Name)) ?? new BlockDataItem();
            if (dataItem.Ignore)
            {
                return new PaletteItem() { BlockName = "", SpriteName = "", BlueprintValue = default(char), MaterialCount = 0 };
            }

            char blueprintChar = default(char);

            if (!dataItem.HideBlueprint)
            {
                var options = $"{name.ToUpperInvariant().Replace(" ", "")}{name.Replace(" ", "")}!@#$%^&*()-_+<>";
                blueprintChar = FindPaletteChar(options);
            }

            var paletteItem = new PaletteItem()
            {
                BlockName = name,
                SpriteName = name,
                BlueprintValue = blueprintChar,
                MaterialCount = 1
            };

            foreach (var variant in dataItem.Variants)
            {
                var match = true;
                foreach (var prop in variant.Props)
                {
                    var propValue = "";
                    if(item.Properties.ContainsKey(prop.Key))
                    {
                        propValue = item.Properties[prop.Key];
                    }
                    if(!Regex.IsMatch(propValue, prop.Value))
                    {
                        match = false;
                    }
                }
                if (match)
                {
                    if(variant.MaterialCount.HasValue)
                    {
                        paletteItem.MaterialCount = variant.MaterialCount.Value;
                    }

                    if(!string.IsNullOrWhiteSpace(variant.BlockName))
                    {
                        paletteItem.BlockName = variant.BlockName.Replace("%name", paletteItem.BlockName);
                    }

                    if(!string.IsNullOrWhiteSpace(variant.SpriteName))
                    {
                        paletteItem.SpriteName = variant.SpriteName.Replace("%name", paletteItem.SpriteName);
                    }
                }
            }

            return paletteItem;
        }

        private char FindPaletteChar(string name)
        {
            int charIndex = 0;
            while(charIndex < name.Length && Palette.Any(x => x.BlueprintValue == name[charIndex] || name[charIndex] == '-'))
            {
                charIndex++;
            }

            if(charIndex < name.Length)
            {
                return name[charIndex];
            }

            throw new Exception($"Could not find valid palette char for name {name}");
        }

        private string CleanSpriteName(string name)
        {
            if(name.StartsWith("minecraft:")) {
                name = name.Substring(10);
            }

            name = name.ToLowerInvariant().Replace("_", " ");

            return Regex.Replace(name, "(^| )([a-z])",
                    s => s.Value.ToUpperInvariant());
        }
    }
}

﻿using NbtToBlueprint.StructureData;
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

        private BlockData BlockData { get; set; }

        public string GenerateBlueprint(StructureDataRaw data, string name)
        {
            var palette = BuildPalette(data);

            var xSize = data.Size[0];
            var ySize = data.Size[1];
            var zSize = data.Size[2];

            var layers = new char[xSize, ySize,zSize];

            var itemCounts = new SortedDictionary<string, int[]>();

            foreach (var block in data.Blocks)
            {
                var paletteItem = palette[block.State];
                var blockName = paletteItem.BlockName;
                if(blockName == "jigsaw")
                {
                    var transformData = block.Nbt["final_state"].ToString().Split('[');
                    var paletteData = new StructureDataRawPalette() { Name = transformData[0], Properties = new Dictionary<string, string>() };
                    if(transformData.Length > 1)
                    {
                        var nbtData = transformData[1].TrimEnd(']');

                        foreach (var dataItem in nbtData.Split(','))
                        {
                            var dataParts = dataItem.Split('=');
                            paletteData.Properties.Add(dataParts[0], dataParts[1]);
                        }
                    }

                    paletteItem = GetPaletteItem(palette, paletteData);
                    var matchingItem = paletteItem = palette.Find(m => m.SpriteName == paletteItem.SpriteName);
                    if(matchingItem == null)
                    {
                        palette.Add(paletteItem);
                    }
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

            var blueprint = new StringBuilder();

            blueprint.AppendLine("{{layered blueprint|name=").Append(name).Append("|default=Layer 1");

            foreach (var item in palette)
            {
                if(item.BlueprintValue != default(char))
                {
                    blueprint.AppendLine($"|{item.BlueprintValue}={item.SpriteName}");
                }
            }
            blueprint.AppendLine();

            var lastLayer = "";
            var lastLayerStart = 0;
            var lastLayerEnd = 0;
            var emptyLayers = new bool[ySize];

            for(var y = 0; y < ySize; y++)
            {
                var currentLayer = new StringBuilder();
                for(var x = xSize - 1; x >= 0; x--)
                {
                    for(var z = 0; z < zSize; z++)
                    {
                        var value = layers[x, y, z];
                        if(value == default(char))
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
                if(string.IsNullOrWhiteSpace(currentLayerString))
                {
                    emptyLayers[y] = true;
                }
                else
                {
                    if(currentLayerString == lastLayer)
                    {
                        lastLayerEnd = y + 1;
                    }
                    else
                    {
                        if(!string.IsNullOrWhiteSpace(lastLayer))
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

            blueprint.AppendLine();
            blueprint.AppendLine();
            blueprint.AppendLine("{| class=\"wikitable sortable mw-collapsible\"");
            blueprint.AppendLine("|-");
            blueprint.Append("!Name");
            for(var y = 0; y < ySize; y++)
            {
                if (!emptyLayers[y])
                {
                    blueprint.Append($" !!Layer {y + 1}");
                }
            }
            blueprint.AppendLine(" !!Total");

            foreach (var item in itemCounts)
            {
                blueprint.AppendLine("|-");
                var formattedName = Regex.Replace(item.Key, "(^|-)([a-z])",
                    s => {
                        var result = "";
                        if(s.Groups[1].Value == "-")
                        {
                            result += " ";
                        }
                        result += s.Groups[2].Value.ToUpperInvariant();
                        return result;
                    });

                blueprint.Append("| {{BlockSprite|").Append(item.Key).Append("|link=").Append(formattedName).Append("|text=").Append(formattedName).Append("}}     ");

                for(var y = 0; y < ySize; y++)
                {
                    if (!emptyLayers[y])
                    {
                        blueprint.Append("|| ");
                        if (item.Value[y] == 0)
                        {
                            blueprint.Append("-");
                        }
                        else
                        {
                            blueprint.Append(item.Value[y]);
                        }
                        blueprint.Append(" ");
                    }
                }
                blueprint.Append("|| ").AppendLine(item.Value.Sum().ToString());
            }

            blueprint.AppendLine("|}");

            return blueprint.ToString();
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

        private List<PaletteItem> BuildPalette(StructureDataRaw data)
        {
            var palette = new List<PaletteItem>();

            foreach (var item in data.Palette)
            {
                palette.Add(GetPaletteItem(palette, item));
            }

            return palette;
        }

        private PaletteItem GetPaletteItem(List<PaletteItem> palette, StructureDataRawPalette item)
        {
            var name = CleanSpriteName(item.Name);
            var dataItem = BlockData.Blocks.FirstOrDefault(i => Regex.IsMatch(name, i.Name)) ?? new BlockDataItem();
            if (dataItem.Ignore)
            {
                return new PaletteItem() { BlockName = "", SpriteName = "", BlueprintValue = default(char), MaterialCount = 0 };
            }

            char validChar = default(char);

            if (!dataItem.HideBlueprint)
            {
                validChar = FindPaletteChar(palette, name.ToUpperInvariant());

                if (validChar == default(char))
                {
                    validChar = FindPaletteChar(palette, name);
                }

                if (validChar == default(char))
                {
                    validChar = FindPaletteChar(palette, "!@#$%^&*()-_+<>");
                }

                if (validChar == default(char))
                {
                    throw new Exception($"Could not find valid palette char for name {name}");
                }
            }

            var paletteItem = new PaletteItem()
            {
                BlockName = name,
                SpriteName = name,
                BlueprintValue = validChar,
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

        private char FindPaletteChar(List<PaletteItem> palette, string name)
        {
            int charIndex = 0;
            while(charIndex < name.Length && palette.Any(x => x.BlueprintValue == name[charIndex] || name[charIndex] == '-'))
            {
                charIndex++;
            }

            if(charIndex < name.Length)
            {
                return name[charIndex];
            }

            return default(char);
        }

        private string CleanSpriteName(string name)
        {
            if(name.StartsWith("minecraft:")) {
                name = name.Substring(10);
            }

            return name.ToLowerInvariant().Replace("_", "-");
        }
    }
}

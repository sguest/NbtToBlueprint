using NbtToBlueprint.StructureData;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbtToBlueprint.Blueprints
{
    public class BlueprintGenerator
    {
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
                var blockName =paletteItem.BlockName;
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

                    var spriteName = GetSpriteName(paletteData);
                    paletteItem = palette.Find(m => m.SpriteName == spriteName);
                    if(paletteItem == null)
                    {
                        paletteItem = GetPaletteItem(palette, paletteData);
                        palette.Add(paletteItem);
                    }
                }
                layers[block.Pos[0], block.Pos[1], block.Pos[2]] = paletteItem.BlueprintValue;

                if(paletteItem.CountForMaterials)
                {
                    if (!itemCounts.ContainsKey(paletteItem.BlockName))
                    {
                        itemCounts.Add(paletteItem.BlockName, new int[ySize]);
                    }

                    itemCounts[paletteItem.BlockName][block.Pos[1]]++;
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
            blueprint.AppendLine("{| class=\"wikitable\"");
            blueprint.AppendLine("|-");
            blueprint.Append("!Name");
            for(var y = 0; y < ySize; y++)
            {
                blueprint.Append($" !!Layer {y + 1}");
            }
            blueprint.AppendLine(" !!Total");

            foreach (var item in itemCounts)
            {
                blueprint.AppendLine("|-");
                var formattedName = System.Text.RegularExpressions.Regex.Replace(item.Key, "(^|-)([a-z])",
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
            if (name == "air" || name == "structure-void")
            {
                return new PaletteItem() { BlockName = "", SpriteName = "", BlueprintValue = default(char), CountForMaterials = false };
            }

            var validChar = findPaletteChar(palette, name.ToUpperInvariant());

            if(validChar == default(char))
            {
                validChar = findPaletteChar(palette, name);
            }

            if(validChar == default(char))
            {
                validChar = findPaletteChar(palette, "!@#$%^&*()-_+<>");
            }

            return new PaletteItem() { BlockName = GetBlockName(item), SpriteName = GetSpriteName(item), BlueprintValue = validChar, CountForMaterials = ShouldCountMaterials(item) };
        }

        private char findPaletteChar(List<PaletteItem> palette, string name)
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

        private string GetBlockName(StructureDataRawPalette paletteData)
        {
            var cleanName = CleanSpriteName(paletteData.Name);

            if(cleanName == "wall-torch")
            {
                return "torch";
            }

            return cleanName;
        }

        private string GetSpriteName(StructureDataRawPalette paletteData)
        {
            var cleanName = CleanSpriteName(paletteData.Name);

            if(cleanName.EndsWith("-log") && (!paletteData.Properties.ContainsKey("axis") ||  paletteData.Properties["axis"] == "y"))
            {
                return cleanName + "-top";
            }

            if (cleanName.EndsWith("-door"))
            {
                if (paletteData.Properties["half"] == "lower")
                {
                    return cleanName + "-bottom";
                }
                if (paletteData.Properties["half"] == "upper")
                {
                    return cleanName + "-top";
                }
            }

            if (cleanName.EndsWith("-stairs"))
            {
                if(paletteData.Properties["half"] == "top")
                {
                    return cleanName + "-rot-180";
                }
            }

            if(cleanName.EndsWith("-bed"))
            {
                var part = paletteData.Properties["part"];
                var facing = paletteData.Properties["facing"];

                if(facing == "north" || facing == "south")
                {
                    cleanName += "-side";
                }
                else
                {
                    cleanName += "-top";
                }

                cleanName += "-" + part;

                if(facing == "east")
                {
                    cleanName += "-rot-90";
                }
                else if(facing == "north")
                {
                    cleanName += "-rot-180";
                }
                else if(facing == "west")
                {
                    cleanName += "-rot-270";
                }
            }

            if(cleanName == "wall-torch")
            {
                switch (paletteData.Properties["facing"])
                {
                    case "north":
                        return cleanName + "-rot-90";
                    case "south":
                        return cleanName + "-rot-270";
                    case "west":
                        return cleanName + "-rot-180";
                }
            }

            if(cleanName == "chest")
            {
                switch (paletteData.Properties["facing"])
                {
                    case "north":
                        return cleanName + "-rot-270";
                    case "south":
                        return cleanName + "-rot-90";
                    case "east":
                        return cleanName + "-rot-180";
                }
            }

            if(cleanName == "glass-pane")
            {
                if (paletteData.Properties["north"] == "true" && paletteData.Properties["south"] == "true")
                {
                    return "glass-pane-rot90";
                }
            }

            if(cleanName == "grass-path" || cleanName == "grass-block" || cleanName == "water")
            {
                return cleanName + "-top";
            }

            return cleanName;
        }

        private string CleanSpriteName(string name)
        {
            if(name.StartsWith("minecraft:")) {
                name = name.Substring(10);
            }

            return name.ToLowerInvariant().Replace("_", "-");
        }

        private bool ShouldCountMaterials(StructureDataRawPalette paletteData)
        {
            var cleanName = CleanSpriteName(paletteData.Name);

            if(cleanName.EndsWith("-bed"))
            {
                return (paletteData.Properties["part"] == "head");
            }

            if(cleanName.EndsWith("-door") || cleanName == "tall-grass")
            {
                return (paletteData.Properties["half"] == "lower");
            }

            return true;
        }
    }
}

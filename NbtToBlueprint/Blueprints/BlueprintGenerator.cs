using NbtToBlueprint.StructureData;
using System;
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

            foreach (var block in data.Blocks)
            {
                var stateValue = palette[block.State].Item1;
                var blockName = palette[block.State].Item2;
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
                    var paletteItem = palette.Find(m => m.Item2 == spriteName);
                    if(paletteItem == null)
                    {
                        stateValue = AddPaletteItem(palette, paletteData);
                    }
                    else
                    {
                        stateValue = paletteItem.Item1;
                    }
                }
                layers[block.Pos[0], block.Pos[1], block.Pos[2]] = stateValue;
            }

            var blueprint = new StringBuilder();

            blueprint.AppendLine("{{layered blueprint|name=").Append(name).Append("|default=Layer 1");

            foreach (var item in palette)
            {
                if(item.Item1 != default(char))
                {
                    blueprint.AppendLine($"|{item.Item1}={item.Item2}");
                }
            }
            blueprint.AppendLine();

            for(var y = 0; y < ySize; y++)
            {
                blueprint.AppendLine($"|----Layer {y + 1}|");
                blueprint.AppendLine();
                for(var x = xSize - 1; x >= 0; x--)
                {
                    for(var z = 0; z < zSize; z++)
                    {
                        var value = layers[x, y, z];
                        if(value == default(char))
                        {
                            blueprint.Append(' ');
                        }
                        else
                        {
                            blueprint.Append(value);
                        }
                    }
                    blueprint.AppendLine();
                }
                blueprint.AppendLine();
            }

            blueprint.AppendLine();
            blueprint.AppendLine("}}");

            return blueprint.ToString();
        }

        private List<Tuple<char, string>> BuildPalette(StructureDataRaw data)
        {
            var palette = new List<Tuple<char, string>>();

            foreach (var item in data.Palette)
            {
                AddPaletteItem(palette, item);
            }

            return palette;
        }

        private char AddPaletteItem(List<Tuple<char, string>> palette, StructureDataRawPalette item)
        {
            char value;
            var name = CleanSpriteName(item.Name);
            if (name == "air" || name == "structure-void")
            {
                value = default(char);
                palette.Add(new Tuple<char, string>(value, ""));
                return value;
            }

            var charIndex = 0;
            while (charIndex < name.Length && (palette.Any(x => x.Item1 == name.ToUpperInvariant()[charIndex]) || name[charIndex] == '-'))
            {
                charIndex++;
            }

            if (charIndex < name.Length)
            {
                value = name.ToUpperInvariant()[charIndex];
                palette.Add(new Tuple<char, string>(value, GetSpriteName(item)));
                return value;
            }

            charIndex = 0;
            while (charIndex < name.Length && (palette.Any(x => x.Item1 == name.ToLowerInvariant()[charIndex] || name[charIndex] == '-')))
            {
                charIndex++;
            }

            value = name[charIndex];
            palette.Add(new Tuple<char, string>(value, GetSpriteName(item)));
            return value;
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
                    case "west":
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
    }
}

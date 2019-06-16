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

            var layers = new char[ySize,xSize,zSize];

            foreach (var block in data.Blocks)
            {
                var stateValue = palette[block.State].Item1;
                var blockName = palette[block.State].Item2;
                if(blockName == "jigsaw")
                {
                    var transformedName = CleanSpriteName(block.Nbt["final_state"].ToString());
                    stateValue = (palette.FirstOrDefault(p => p.Item2 == transformedName)?.Item1 ?? ' ' as char?).Value;
                }
                layers[block.Pos[1], block.Pos[0], block.Pos[2]] = stateValue;
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
                for(var x = 0; x < xSize; x++)
                {
                    for(var z = 0; z < zSize; z++)
                    {
                        var value = layers[y, x, z];
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
                var name = CleanSpriteName(item.Name);
                if(name == "air")
                {
                    palette.Add(new Tuple<char, string>(default(char), ""));
                    continue;
                }

                var charIndex = 0;
                while (charIndex < name.Length && (palette.Any(x => x.Item1 == name.ToUpperInvariant()[charIndex]) || name[charIndex] == '-'))
                {
                    charIndex++;
                }

                if(charIndex < name.Length)
                {
                    palette.Add(new Tuple<char, string>(name.ToUpperInvariant()[charIndex], GetSpriteName(item)));
                    continue;
                }

                charIndex = 0;
                while (charIndex < name.Length && (palette.Any(x => x.Item1 == name.ToLowerInvariant()[charIndex] || name[charIndex] == '-')))
                {
                    charIndex++;
                }

                palette.Add(new Tuple<char, string>(name[charIndex], GetSpriteName(item)));
            }

            return palette;
        }

        private string GetSpriteName(StructureDataRawPalette paletteData)
        {
            var cleanName = CleanSpriteName(paletteData.Name);

            if(cleanName.EndsWith("-log") && paletteData.Properties["axis"] == "y")
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
                    case "east":
                        return cleanName + "-rot-180";
                }
            }

            if(cleanName == "chest" || cleanName.EndsWith("-stairs"))
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

            if(cleanName == "grass-path" || cleanName == "grass-block")
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

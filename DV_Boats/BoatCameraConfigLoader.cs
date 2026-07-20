using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DV_Boats
{
    internal static class BoatCameraConfigLoader
    {
        public static Dictionary<string, Dictionary<string, CameraViewDefinition>> camerasByBoat =
        new Dictionary<string, Dictionary<string, CameraViewDefinition>>();

        private static bool loaded = false;
        private static string GetConfigPath()
        {
            string modsFolder = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Mods",
                "DV_Boats"
            );

            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            return Path.Combine(modsFolder, "DV_Boats_cameras.json");
        }

        public static void EnsureLoaded()
        {
            Main.Log("[BoatCameraConfig] EnsureLoaded() entered. loaded=" + loaded);

            string path = GetConfigPath();
            Main.Log("[BoatCameraConfig] Looking for JSON at: " + path);

            if (!File.Exists(path))
            {
                Main.Log("[BoatCameraConfig] No JSON found. Creating default file...");
                CreateDefaultFile(path);
            }

            string jsonText;
            try
            {
                jsonText = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Main.Log("[BoatCameraConfig] ❌ Failed to read file: " + ex.Message);
                camerasByBoat.Clear();
                loaded = false;
                return;
            }

            camerasByBoat.Clear();

            if (string.IsNullOrEmpty(jsonText))
            {
                Main.Log("[BoatCameraConfig] ❌ Empty JSON text.");
                loaded = false;
                return;
            }

            camerasByBoat.Clear();

            int idx = jsonText.IndexOf("\"boatCameraProfiles\"", StringComparison.Ordinal);
            if (idx < 0)
            {
                Main.Log("[BoatCameraConfig] ❌ No \"boatCameraProfiles\" property found.");
                loaded = false;
                return;
            }

            int objStart = jsonText.IndexOf('{', idx);
            if (objStart < 0)
            {
                Main.Log("[BoatCameraConfig] ❌ No '{' after boatCameraProfiles.");
                loaded = false;
                return;
            }

            int objEnd = FindMatchingBracket(jsonText, objStart, '{', '}');
            if (objEnd < 0)
            {
                Main.Log("[BoatCameraConfig] ❌ Could not find closing '}' for boatCameraProfiles.");
                loaded = false;
                return;
            }

            string profilesBody = jsonText.Substring(
                objStart + 1,
                objEnd - objStart - 1
            );

            var boatArrays = ExtractNamedArrays(profilesBody);

            int totalAdded = 0;

            foreach (var boatEntry in boatArrays)
            {
                string boatId = boatEntry.Key;
                string arrayText = boatEntry.Value;

                var camDict = new Dictionary<string, CameraViewDefinition>();

                List<string> camBlocks = new List<string>();
                int pos = 0;

                while (pos < arrayText.Length)
                {
                    int braceStart = arrayText.IndexOf('{', pos);
                    if (braceStart < 0)
                        break;

                    int braceEnd = FindMatchingBracket(arrayText, braceStart, '{', '}');
                    if (braceEnd < 0)
                        break;

                    camBlocks.Add(arrayText.Substring(braceStart, braceEnd - braceStart + 1));
                    pos = braceEnd + 1;
                }

                foreach (string block in camBlocks)
                {
                    string name =
                        ExtractString(block, "label") ??
                        ExtractString(block, "name") ??
                        ExtractString(block, "id");

                    if (string.IsNullOrEmpty(name))
                        continue;

                    string mode = ExtractString(block, "mode");
                    Vector3 offset = ParseVector3(block, "offset", Vector3.zero);
                    Vector3 lookAtOffset = ParseVector3(block, "lookAtOffset", Vector3.zero);
                    bool useCabPivot = ExtractBool(block, "useCabPivot", false);

                    camDict[name] = new CameraViewDefinition
                    {
                        name = name,
                        mode = mode,
                        offset = offset,
                        lookAtOffset = lookAtOffset,
                        useCabPivot = useCabPivot
                    };

                    totalAdded++;
                }

                camerasByBoat[boatId] = camDict;
            }

            Main.Log("[BoatCameraConfig] Loaded " + totalAdded + " camera views across " +
                     camerasByBoat.Count + " boat profiles.");

            loaded = true;
        }

        public static void Reload()
        {
            loaded = false;
            EnsureLoaded();
        }

        private static void CreateDefaultFile(string path)
        {
            const string defaultJson =
            @"{
              ""boatCameraProfiles"": {
                ""FishingBoat_01"": [
                  {
                    ""name"": ""Boat Chase Rear"",
                    ""offset"": { ""x"": 0.0, ""y"": 10.0, ""z"": -30.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Rear Deck"",
                    ""offset"": { ""x"": 0.0, ""y"": 6.0, ""z"": -7.5 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Flybridge"",
                    ""offset"": { ""x"": 0.0, ""y"": 11.2, ""z"": 8.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Bow Forward"",
                    ""offset"": { ""x"": 0.0, ""y"": 8.0, ""z"": 15.25 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Boat Side Left"",
                    ""offset"": { ""x"": -30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Side Right"",
                    ""offset"": { ""x"": 30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Chase Front"",
                    ""offset"": { ""x"": 0.0, ""y"": 6.0, ""z"": 36.0 },
                    ""mode"": ""LookBackward""
                  }
                ],

                ""FishingBoat_02"": [
                  {
                    ""name"": ""Boat Chase Rear"",
                    ""offset"": { ""x"": 0.0, ""y"": 10.0, ""z"": -24.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Rear Deck"",
                    ""offset"": { ""x"": 0.0, ""y"": 6.0, ""z"": -12.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Flybridge"",
                    ""offset"": { ""x"": 0.0, ""y"": 9.2, ""z"": 8.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Bow Forward"",
                    ""offset"": { ""x"": 0.0, ""y"": 8.0, ""z"": 12.25 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Boat Side Left"",
                    ""offset"": { ""x"": -30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Side Right"",
                    ""offset"": { ""x"": 30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Chase Front"",
                    ""offset"": { ""x"": 0.0, ""y"": 6.0, ""z"": 36.0 },
                    ""mode"": ""LookBackward""
                  }
                ],

                ""FishingBoat_03"": [
                  {
                    ""name"": ""Boat Chase Rear"",
                    ""offset"": { ""x"": 0.0, ""y"": 10.0, ""z"": -24.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Rear Deck"",
                    ""offset"": { ""x"": 0.0, ""y"": 7.0, ""z"": -13.5 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Rooftop"",
                    ""offset"": { ""x"": 0.0, ""y"": 11.0, ""z"": 3.0 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Bow Forward"",
                    ""offset"": { ""x"": 0.0, ""y"": 8.5, ""z"": 12.25 },
                    ""mode"": ""LookForward""
                  },
                  {
                    ""name"": ""Boat Side Left"",
                    ""offset"": { ""x"": -30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Side Right"",
                    ""offset"": { ""x"": 30.0, ""y"": 7.0, ""z"": 0.0 },
                    ""mode"": ""LookAtLoco""
                  },
                  {
                    ""name"": ""Boat Chase Front"",
                    ""offset"": { ""x"": 0.0, ""y"": 6.0, ""z"": 36.0 },
                    ""mode"": ""LookBackward""
                  }
                ]
              }
            }";


            try
            {
                File.WriteAllText(path, defaultJson, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Main.Log("[BoatCameraConfig] ❌ Failed to write default JSON: " + ex.Message);
            }
        }

        private static int FindMatchingBracket(string text, int startIndex, char open, char close)
        {
            int depth = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == open) depth++;
                else if (text[i] == close)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        private static string ExtractString(string block, string key)
        {
            Match m = Regex.Match(block, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static Vector3 ParseVector3(string block, string key, Vector3 def)
        {
            int idx = block.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (idx < 0) return def;

            int braceStart = block.IndexOf('{', idx);
            int braceEnd = FindMatchingBracket(block, braceStart, '{', '}');
            if (braceStart < 0 || braceEnd < 0) return def;

            string obj = block.Substring(braceStart + 1, braceEnd - braceStart - 1);

            float x = ExtractFloat(obj, "\"x\"");
            float y = ExtractFloat(obj, "\"y\"");
            float z = ExtractFloat(obj, "\"z\"");

            return new Vector3(x, y, z);
        }

        private static float ExtractFloat(string text, string key)
        {
            Match m = Regex.Match(text, key + "\\s*:\\s*([-+]?[0-9]*\\.?[0-9]+)");
            if (!m.Success) return 0f;

            float.TryParse(
                m.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value
            );
            return value;
        }

        private static bool ExtractBool(string block, string key, bool def)
        {
            Match m = Regex.Match(block, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return m.Success
                ? m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase)
                : def;
        }

        private static Dictionary<string, string> ExtractNamedArrays(string objectBody)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            int pos = 0;
            while (pos < objectBody.Length)
            {
                Match keyMatch = Regex.Match(
                    objectBody.Substring(pos),
                    "\"([^\"]+)\"\\s*:"
                );

                if (!keyMatch.Success)
                    break;

                string key = keyMatch.Groups[1].Value;
                int keyIndex = pos + keyMatch.Index;

                int arrayStart = objectBody.IndexOf('[', keyIndex);
                if (arrayStart < 0)
                    break;

                int arrayEnd = FindMatchingBracket(objectBody, arrayStart, '[', ']');
                if (arrayEnd < 0)
                    break;

                string arrayText = objectBody.Substring(
                    arrayStart,
                    arrayEnd - arrayStart + 1
                );

                result[key] = arrayText;
                pos = arrayEnd + 1;
            }

            return result;
        }

    }
}

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OptimisationTool
{
    public sealed class DuplicatesFinder
    {
        public Dictionary<string, List<string>> FindDuplicates(IEnumerable<string> files)
        {
            Dictionary<string, List<string>> textureMap = new();
            Dictionary<string, List<string>> duplicateGroups = new();

            foreach (var file in files)
            {
                Texture2D textureFile = AssetDatabase.LoadAssetAtPath(file, typeof(Texture2D)) as Texture2D;

                if (textureFile == null)
                {
                    continue;
                }

                byte[] textureBytes = File.ReadAllBytes(file);

                string hash = GetTextureHash(textureBytes);

                if (!textureMap.ContainsKey(hash))
                {
                    textureMap[hash] = new List<string>();
                }

                textureMap[hash].Add(AssetDatabase.GetAssetPath(textureFile));
            }

            foreach (var group in textureMap)
            {
                if (group.Value.Count > 1)
                {
                    string groupKey = Path.GetFileName(group.Value[0]);

                    if (!duplicateGroups.ContainsKey(groupKey))
                    {
                        duplicateGroups[groupKey] = new List<string>();
                    }

                    duplicateGroups[groupKey] = group.Value;
                }
            }

            return duplicateGroups;
        }

        public bool PairExists(Dictionary<string, List<string>> duplicatesGroups)
        {
            var totalPairs = 0;

            foreach (var group in duplicatesGroups)
            {
                foreach (var file in duplicatesGroups.Values)
                {
                    if (file.Count > 1)
                    {
                        totalPairs++;
                    }
                }
            }

            if (totalPairs > 0)
            {
                return true;
            }

            return false;
        }

        private string GetTextureHash(byte[] bytes)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashByte = md5.ComputeHash(bytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashByte.Length; i++)
                {
                    sb.Append(hashByte[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace OptimisationTool
{
    public sealed class TextureOptimizer
    {
        private long _threshold;

        public TextureOptimizer(long threshold)
        {
            _threshold = threshold;
        }

        public List<string> GetNotOptimizedFiles(IEnumerable<string> files)
        {
            var filesToOptimize = new List<string>();

            foreach (var filePath in files)
            {
                TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;

                if (textureImporter == null)
                {
                    continue;
                }
                TextureImporterCompression originalCompression = textureImporter.textureCompression;

                textureImporter.textureCompression = TextureImporterCompression.Compressed;

                Texture2D textureFile = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D)) as Texture2D;
                var fileSize = Profiler.GetRuntimeMemorySizeLong(textureFile);

                textureImporter.textureCompression = originalCompression;

                if (fileSize < _threshold)
                {
                    continue;
                }
                lock (filesToOptimize)
                {
                    filesToOptimize.Add(filePath);
                }
            }

            return filesToOptimize;
        }
    }
}

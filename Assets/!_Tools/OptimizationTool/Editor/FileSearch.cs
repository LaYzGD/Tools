using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OptimisationTool
{
    public class FileSearch
    {
        private bool _pngFormatEnabled = true;
        private bool _jpegFormatEnabled = true;
        private bool _jpgFormatEnabled = true;
        private string _graphicsFolder;

        public FileSearch(bool pngFormatEnabled, bool jpegFormatEnabled, bool jpgFormatEnabled, string path)
        {
            _pngFormatEnabled = pngFormatEnabled;
            _jpegFormatEnabled = jpegFormatEnabled;
            _jpgFormatEnabled = jpgFormatEnabled;
            _graphicsFolder = path;
        }

        public IEnumerable<string> Search()
        {
            var formats = new List<string>();

            if (_pngFormatEnabled)
            {
                formats.Add("png");
            }
            if (_jpegFormatEnabled)
            {
                formats.Add("jpeg");
            }
            if (_jpgFormatEnabled)
            {
                formats.Add("jpg");
            }

            var files = Directory
                .GetFiles(_graphicsFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => formats.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));

            return files;
        }
    }
}
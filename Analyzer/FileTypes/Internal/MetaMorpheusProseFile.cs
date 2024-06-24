using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;

namespace Analyzer.FileTypes.Internal
{
    public class MetaMorpheusProseFile
    {
        public string FilePath { get; init; }
        public string[] SpectraFilePaths { get; private set; }
        public string[] DatabasePaths { get; private set; }

        public MetaMorpheusProseFile(string filePath)
        {
            FilePath = filePath;
            SpectraFilePaths = Array.Empty<string>();
            DatabasePaths = Array.Empty<string>();
            ParseAllInfo();
        }

        private void ParseAllInfo()
        {
            var lines = File.ReadAllLines(FilePath);
            var specIndex = lines.IndexOf("Spectra files: ");
            var dbIndex = lines.IndexOf("Databases:");


            var spec = new List<string>();
            var db = new List<string>();
            for (int i = specIndex+1; i < dbIndex; i++)
            {
                spec.Add(lines[i].Trim());
            }

            for (int i = dbIndex+1; i < lines.Length; i++)
            {
                db.Add(lines[i].Trim());
            }
            SpectraFilePaths = spec.ToArray();
            DatabasePaths = db.ToArray();
        }
    }
}

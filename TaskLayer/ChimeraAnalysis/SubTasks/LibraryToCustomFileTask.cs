using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskLayer.ChimeraAnalysis
{
    public class LibraryToCustomFileTask : BaseResultAnalyzerTask
    {
        public override MyTask MyTask => MyTask.LibraryToCustomMzml;
        public override BaseResultAnalyzerTaskParameters Parameters { get; }

        public LibraryToCustomFileTask(BaseResultAnalyzerTaskParameters parameters)
        {
            Parameters = parameters;
            Condition = Path.GetFileNameWithoutExtension(parameters.InputDirectoryPath);
        }

        /// <summary>
        /// take a spectral library and creates a custom mzml and index file for it
        ///
        /// Create artificial chimeric spectra from the library
        /// Vary the degree of chimerism and the relative intensity of each in ms1 and ms2
        /// </summary>
        protected override void RunSpecific()
        {
            var libraryPath = Parameters.InputDirectoryPath;
            if (libraryPath == null)
                throw new ArgumentNullException("libraryPath", "Library path is null");
            if (!libraryPath.EndsWith(".msp"))
                throw new ArgumentException("Library path must end with .msp");

            string libName = Path.GetFileNameWithoutExtension(libraryPath);
            string libDir = Path.GetDirectoryName(libraryPath);
            string mzmlOutPath = Path.Combine(libDir, libName + ".mzML");
            string indexOutPath = Path.Combine(libDir, libName + ".csv");

            if (File.Exists(mzmlOutPath) && File.Exists(indexOutPath) && !Parameters.Override)
            {
                Log("Custom MzML already found for library, stopping task");
                return;
            }

            // TODO: Actually make the file :(

            throw new NotImplementedException();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;

namespace Analyzer.SearchType
{
    internal class ChimerysResult : BulkResult
    {
        private ProteomeDiscovererPsmFile _psmFile;
        private ProteomeDiscovererProteoformFile _peptideFile;
        private ProteomeDiscovererProteinFile _proteinFile;
        private string _inputFilePath;
        private ProteomeDiscovererInputFileFile _inputFile;
        private Dictionary<string, string> _idToFileNameDictionary;



        public ProteomeDiscovererInputFileFile InputFile => _inputFile ??= new ProteomeDiscovererInputFileFile(_inputFilePath);



        public ChimerysResult(string directoryPath) : base(directoryPath)
        {
            IsTopDown = false;
        }

        public override BulkResultCountComparisonFile IndividualFileComparison(string path = null)
        {
            throw new NotImplementedException();
        }

        public override ChimeraCountingFile CountChimericPsms()
        {
            throw new NotImplementedException();
        }

        public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.External;
using Analyzer.FileTypes.Internal;

namespace Analyzer.SearchType;

public class ChimerysResult : SingleRunResults
{

    private ChimerysResultDirectory _chimerysResultDirectory;

    public ChimerysResultDirectory ChimerysResultDirectory
    {
        get => _chimerysResultDirectory;
        set
        {
            _chimerysResultDirectory = value;
            DirectoryPath = value.DirectoryPath;
        }
    }

    public ChimerysResult(string directoryPath, string? datasetName = null, string? condition = null) : base(directoryPath, datasetName, condition)
    {
        IsTopDown = false;
        ChimerysResultDirectory = new(directoryPath);
    }

    public override BulkResultCountComparisonFile? GetIndividualFileComparison(string path = null)
    {
        if (!Override && File.Exists(_IndividualFilePath))
            return new BulkResultCountComparisonFile(_IndividualFilePath);

        throw new NotImplementedException();
    }

    public override ChimeraCountingFile CountChimericPsms()
    {
        if (!Override && File.Exists(_chimeraPsmPath))
            return new ChimeraCountingFile(_chimeraPsmPath);

        throw new NotImplementedException();
    }

    public override BulkResultCountComparisonFile GetBulkResultCountComparisonFile(string path = null)
    {
        if (!Override && File.Exists(_bulkResultCountComparisonPath))
            return new BulkResultCountComparisonFile(_bulkResultCountComparisonPath);

        throw new NotImplementedException();
    }

    public override ProformaFile ToPsmProformaFile()
    {
        if (File.Exists(_proformaPsmFilePath) && !Override)
            return _proformaPsmFile ??= new ProformaFile(_proformaPsmFilePath);

        throw new NotImplementedException();
    }

    public override ProteinCountingFile CountProteins()
    {
        if (File.Exists(_proteinCountingFilePath) && !Override)
            return _proteinCountingFile ??= new ProteinCountingFile(_proteinCountingFilePath);

        throw new NotImplementedException();
    }
}
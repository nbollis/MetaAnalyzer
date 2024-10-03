using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsPathFinderTRunner
{
    internal class MsPathFinderTParams
    {
        internal string InputDirectory { get; set; }
        internal string OutputDirectory { get; set; }
        internal string DatabasePath { get; set; }
        internal string ModFilePath { get; set; }
        internal int MaxIdsPerSpectra { get; set; }

        internal static List<MsPathFinderTParams> AllParamsLeftToRun => new List<MsPathFinderTParams>()
        {
            new MsPathFinderTParams()
            {
                InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderT",
                OutputDirectory =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithMods_15Rep2_Final",
                DatabasePath =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_25.fasta",
                ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\InformedProteomicsMods.txt",
                MaxIdsPerSpectra = 15,
            },
            //new MsPathFinderTParams()
            //{
            //    InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderT",
            //    OutputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithModsNoChimeras",
            //    DatabasePath =  @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_25.fasta",
            //    ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\InformedProteomicsMods.txt",
            //    MaxIdsPerSpectra = 1,
            //},
            new MsPathFinderTParams()
            {
                InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderT",
                OutputDirectory =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderTWithMods_15_Final",
                DatabasePath =
                    @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\Ecoli_uniprotkb_proteome_UP000000625_AND_revi_2024_04_04.fasta",
                ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\InformedProteomicsMods.txt",
                MaxIdsPerSpectra = 15,
            },
            //new MsPathFinderTParams()
            //{
            //    InputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderT",
            //    OutputDirectory = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderTWithModsNoChimeras",
            //    DatabasePath =  @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\Ecoli_uniprotkb_proteome_UP000000625_AND_revi_2024_04_04.fasta",
            //    ModFilePath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\InformedProteomicsMods.txt",
            //    MaxIdsPerSpectra = 1,
            //},
        };
    }
}

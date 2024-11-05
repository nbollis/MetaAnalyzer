using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plotly.NET;
using RadicalFragmentation;

namespace Test
{
    [TestFixture]
    internal class RadicalFragmentation
    {
        internal static string HumanDatabasePath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
        internal static string YeastDatabasePath = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_yeast_proteome_AND_model_orga_2024_03_27.xml";
        internal static string EcoliDatabase = @"D:\Projects\RadicalFragmentation\FragmentAnalysis\Databases\uniprotkb_ecoli_proteome_AND_reviewed_t_2024_03_25.xml";
        internal static string OutputDirectory = @"D:\Projects\RadicalFragmentation\FragmentAnalysis";

        [Test]
        public static void RunDbPlot()
        {
            var path = HumanDatabasePath;
            var plot = new DatabaseMassPlot(path, 2);
            var chart = plot.GeneratePlot();
            chart.Show();
        }
    }
}

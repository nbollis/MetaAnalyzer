using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonteCarlo;

public interface IRecord
{
    string Condition { get; set; }
}

public class HistogramRecord : IRecord
{
    public string Condition { get; set; }
    public double Score { get; set; }
    public int Count { get; set; }
}

public class AllScoreRecord : IRecord
{
    public AllScoreRecord() { }
    public AllScoreRecord(string condition, int iteration, double score)
    {
        Condition = condition;
        Iteration = iteration;
        Score = score;
    }

    public string Condition { get; set; }
    public int Iteration { get; set; }
    public double Score { get; set; }
}

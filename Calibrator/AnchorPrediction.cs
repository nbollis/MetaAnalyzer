using Microsoft.ML.Data;

namespace Calibrator;

public class AnchorPrediction
{
    [ColumnName("Score")] public float TransformedRetentionTime { get; set; }
}
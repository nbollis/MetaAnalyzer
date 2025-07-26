
namespace ResultAnalyzerUtil
{


    public class CustomComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, object>[] propertySelectors;

        public CustomComparer(params Func<T, object>[] propertySelectors)
        {
            this.propertySelectors = propertySelectors;
        }

        public bool Equals(T x, T y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;

            foreach (var selector in propertySelectors)
            {
                if (selector.Target is IEnumerable<double> enumerable)
                {
                    if (!enumerable.SequenceEqual((IEnumerable<double>)selector(y)))
                        return false;
                }
                if (selector.Target is IEnumerable<int> enumerableInt)
                {
                    if (!enumerableInt.SequenceEqual((IEnumerable<int>)selector(y)))
                        return false;
                }
                else if (!Equals(selector(x), selector(y)))
                    return false;
            }

            return true;
        }

        public int GetHashCode(T obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var selector in propertySelectors)
                {
                    hash = hash * 23 + (selector(obj)?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }


        #region Custom Implementations

        // MetaMorpheus
        public static CustomComparer<PsmFromTsv> ChimeraComparer =>
            new(psm => psm.PrecursorScanNum, psm => psm.Ms2ScanNumber,
                psm => psm.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", ""));

        public static CustomComparer<PsmFromTsv> MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer =>
            new(psm => psm.FullSequence, psm => psm.Ms2ScanNumber, psm => psm.PrecursorScanNum,
                psm => psm.FileNameWithoutExtension.Replace("-calib", "").Replace("-averaged", "")); 

        
        #endregion

    }
}

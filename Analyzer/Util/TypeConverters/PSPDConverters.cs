using AnalyzerCore;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MzLibUtil;
using System.Text.RegularExpressions;
using ResultAnalyzerUtil;
using Omics.Modifications;

namespace Analyzer.Util.TypeConverters;

public class PSPDMsOrderConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return int.Parse(text.Last().ToString());
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        return $"MS{value}";
    }
}

public class ChimerysFullSequenceToModificationConverter : DefaultTypeConverter
{
    private static readonly Regex Regex = new Regex(@"(?<=\[)[^\]]+(?=\])");
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        var mods = new Dictionary<int, Modification>();
        if (string.IsNullOrEmpty(text))
            return mods.ToArray();

        var baseSequence = row.GetField<string>("SEQUENCE");
        var modMatches = Regex.Matches(text, @"\[(.*?)\]");

        int removedOffset = 0;

        foreach (Match match in modMatches)
        {
            string mod = match.Groups[1].Value;
            int uniModId = int.Parse(mod.Split(':')[1]);

            int modifiedIndex;
            char modifiedResidue;

            if (match.Index == 0 || (match.Index > 0 && text[match.Index - 1] == '-'))
            {
                // N-terminal mod (either at position 0 or prefixed with '-')
                modifiedIndex = 0;
                modifiedResidue = 'X';
                removedOffset++;
            }
            else
            {
                // Index in original string of the '['
                int bracketIndex = match.Index;

                // The index of the amino acid just before the '[' in the original string
                int aaIndexInOriginal = bracketIndex - 1;

                // Index in the cleaned string = remove chars before this mod: all previous removed content
                int adjustedIndex = aaIndexInOriginal - removedOffset;

                modifiedIndex = adjustedIndex + 1;
                modifiedResidue = baseSequence![adjustedIndex];
            }

            var mmMod = ILocalizedModification.GetClosestMod(uniModId, modifiedResidue, GlobalVariables.AllModsKnown);
            mods.Add(modifiedIndex, mmMod);

            // Update offset to account for removed [mod]
            removedOffset += match.Length;
        }

        return mods;
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        var sequence = value as string ?? throw new MzLibException("Cannot convert input to string");
        return sequence;
    }
}
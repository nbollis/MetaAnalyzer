using System.Text;
using System.Text.RegularExpressions;
using Analyzer.FileTypes.External;
using Analyzer.SearchType;
using AnalyzerCore;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Analyzer.Util.TypeConverters
{
    /// <summary>
    /// Converts the chemical formula from MsPathFinderT to MetaMorpheus
    /// MsPathFinderT: "C(460) H(740) N(136) O(146) Message(0)"
    /// MetaMorpheus: "C460H740N136O146S"
    /// </summary>
    public class MsPathFinderTCompositionToChemicalFormulaConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var regex = new Regex(@"([A-Z][a-z]*)(\((\d+)\))?");
            var matches = regex.Matches(text);
            var chemicalFormula = new Chemistry.ChemicalFormula();

            foreach (Match match in matches)
            {
                var elementName = match.Groups[1].Value;
                var elementCount = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 1;
                chemicalFormula.Add(elementName, elementCount);
            }

            return chemicalFormula;
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var chemicalFormula = value as Chemistry.ChemicalFormula ?? throw new Exception("Cannot convert input to ChemicalFormula");
            var sb = new StringBuilder();

            bool onNumber = false;
            foreach (var character in chemicalFormula.Formula)
            {
                if (!char.IsDigit(character)) // if is a letter
                {
                    if (onNumber)
                    {
                        sb.Append(") " + character);
                        onNumber = false;
                    }
                    else
                        sb.Append(character);
                }
                else
                {
                    if (!onNumber)
                    {
                        sb.Append("(" + character);
                        onNumber = true;
                    }
                    else
                        sb.Append(character);
                }
            }

            var stringForm = sb.ToString();
            if (char.IsDigit(stringForm.Last()))
                stringForm += ")";
            else
                stringForm += "(1)";

            return stringForm;
        }
    }

    public class MsPathFinderTPsmStringToModificationsArrayConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var mods = new List<MsPathFinderTModification>();
            if (string.IsNullOrEmpty(text))
                return mods.ToArray();

            var modStrings = text.Split(',');
            foreach (var modString in modStrings)
            {
                if (!modString.Contains(" "))
                {
                    continue;
                }
                var modSplits = modString.Split(' ');
                var name = modSplits[0];
                var location = int.Parse(modSplits[1]);

                var baseSequence = row.GetField<string>("Sequence");
                var modifiedResidue = location == 0 ? 'X' : baseSequence[location - 1];
                var mass = ILocalizedModification.GetNominalMass(name, modifiedResidue);

                mods.Add(new MsPathFinderTModification(name, location, modifiedResidue, mass));
            }

            return mods.ToArray();
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is MsPathFinderTModification[] modifications)
            {
                var modStrings = modifications.Select(mod => $"{mod.Name} {mod.OneBasedLocalization}");
                return string.Join(",", modStrings);
            }
            return "";
        }
    }
}

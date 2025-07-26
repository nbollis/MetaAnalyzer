using System.Text.RegularExpressions;
using Analyzer.SearchType;
using AnalyzerCore;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Analyzer.Util.TypeConverters
{
    /// <summary>
    /// Converts Proteome discoveror annotated sequence to base sequence by keeping only capital letters
    /// </summary>
    public class ProteomeDiscovererAnnotatedToBaseSequenceConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return new string(text.Where(char.IsLetter).Select(char.ToUpper).ToArray());
        }
    }

    /// <summary>
    /// Second Case
    /// Converts mods with format C15(Carbamidomethyl); C16(Carbamidomethyl) to
    /// ProteoformDiscovererModification with Modifications  ProteomeDiscovererModification(15, "C", "Carbamidomethyl"), new ProteomeDiscovererModification(16, "C", "Carbamidomethyl") }
    ///
    /// First Case
    /// 1xCarbamidomethyl [C12]  -> 12, C, CarbamidoMethyl
    /// cc -> 12, C, Carbamidomethyl; 15, C, Carbamidomethyl
    /// 1xCarbamidomethyl [C12]; 1xOxidation [M15] -> 12, C, Carbamidomethyl; 15, M, Oxidation
    /// 
    /// </summary>
    public class ProteomeDiscovererPSMModToProteomeDiscovererModificationArrayConverter : DefaultTypeConverter
    {
        
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var proteomeDiscovererMods = new List<ProteomeDiscovererMod>();
            if (string.IsNullOrEmpty(text))
                return proteomeDiscovererMods.ToArray();
            else if (char.IsDigit(text.First())) // Chimerys
            {
                string pattern = @"\d(.*?)\]";
                MatchCollection matches = Regex.Matches(text, pattern);

                // Iterate over each match
                foreach (Match match in matches)
                {
                    var modParts = match.Value.Split(" [");
                    string modName = modParts[0].Substring(2);
                    
                    foreach (var loc in modParts[1].Split(';').Select(p => p.Trim().Replace("]", "")))
                    {
                        if (!int.TryParse(loc.Trim().Substring(1), out int modLocation))
                            modLocation = 1;
                        
                        var modifiedResidue = loc.Trim()[0]; 
                        var mass = ILocalizedModification.GetNominalMass(modName, modifiedResidue);
                        proteomeDiscovererMods.Add(new ProteomeDiscovererMod(modLocation, modName, modifiedResidue, mass));
                    }
                }
            }
            else // Prosight
            {
                var mods = text.Split(';');
                foreach (var mod in mods)
                {
                    var match = Regex.Match(mod, @"([A-Z])(\d+)\((.+)\)");
                    if (match.Success)
                    {
                        var modifiedResidue = match.Groups[1].Value[0];
                        var modLocation = int.Parse(match.Groups[2].Value);
                        var modName = match.Groups[3].Value;
                        var mass = ILocalizedModification.GetNominalMass(modName, modifiedResidue);
                        proteomeDiscovererMods.Add(new ProteomeDiscovererMod(modLocation, modName, modifiedResidue, mass));
                    }
                }
            }

            return proteomeDiscovererMods.ToArray();
        }
    }

}

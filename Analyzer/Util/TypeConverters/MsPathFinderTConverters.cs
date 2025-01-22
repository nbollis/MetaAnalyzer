﻿using System.Text;
using Analyzer.FileTypes.External;
using Analyzer.SearchType;
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
            var composition = text.Split(' ').Where(p => p != "").ToArray();
            var chemicalFormula = new Chemistry.ChemicalFormula();
            foreach (var element in composition)
            {
                var elementSplit = element.Split('(');
                var elementName = elementSplit[0];
                var elementCount = int.Parse(elementSplit[1].Replace(")", ""));
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
                var modSplits = modString.Split(' ');
                var name = modSplits[0];
                var location = int.Parse(modSplits[1]);

                var mass = name switch
                {
                    "Carbamidomethyl" => 57,
                    "Oxidation" => 16,
                    "Phospho" => 80,
                    "Acetyl" => 42,
                    "Methyl" => 14,
                    _ => throw new ArgumentOutOfRangeException()
                };

                mods.Add(new MsPathFinderTModification(name, location, mass));
            }

            return mods.ToArray();
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            return base.ConvertToString(value, row, memberMapData);
        }
    }
}

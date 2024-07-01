using CommandLine;

internal class CommandLineArguments
{
    public List<string> Spectra { get; private set; }

    [Option('s', HelpText = "Spectra to analyze (.raw, .mzML, .mgf file formats) or folder(s) containing spectra; space-delimited", Required = true)]
    public IEnumerable<string> _spectra { get; set; }

    [Option('o', HelpText = "The output Directory", Required = false, Default = false)]
    public bool OverrideFiles { get; set; }

    [Option('o', HelpText = "[Optional] Output folder", Required = true)]
    public string OutputFolder { get; set; }

    [Option('z', HelpText = "[Optional] Maximum charge state to consider", Required = false, Default = 60)]
    public int MaxCharge { get; set; }

    public void ValidateCommandLineSettings()
    {
        if (_spectra == null)
        {
            throw new Exception("No spectra specified");
        }
        Spectra = _spectra.ToList();

        if (Spectra.Any(s => !File.Exists(s) && !Directory.Exists(s)))
        {
            throw new Exception("One or more spectra do not exist");
        }

        if (string.IsNullOrWhiteSpace(OutputFolder))
        {
            throw new Exception("No output folder specified");
        }

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        if (MaxCharge < 1)
        {
            throw new Exception("Max charge must be greater than 0");
        }
    }
}
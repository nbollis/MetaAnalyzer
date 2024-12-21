using Plotly.NET;

namespace Plotting.RadicalFragmentation
{
    public static class RadicalFragmentationPlotHelpers
    {
        public static ColorQueue<Color> ColorQueue;

        static RadicalFragmentationPlotHelpers()
        {
            var missedMonos = new List<int> { 0, 1, 2, 3 };
            ColorQueue = new PlotlyColorQueue();

            ModAndMissedMonoToColorDict = new Dictionary<int, Dictionary<int, Color>>();
            ModToColorSetDict = new Dictionary<int, List<Color>>();
            foreach (var mod in ModToColorDict.Keys)
            {
                var missedMonoToColorDict = new Dictionary<int, Color>();
                var set = ColorQueue.DequeueSet();

                foreach (var missedMono in missedMonos)
                {
                    missedMonoToColorDict[missedMono] = set[missedMono];
                }
                ModAndMissedMonoToColorDict[mod] = missedMonoToColorDict;
                ModToColorSetDict[mod] = set;
            }
        }

        public static Dictionary<int, Color> ModToColorDict = new Dictionary<int, Color>()
        {
            {0, Color.fromKeyword(ColorKeyword.RoyalBlue) },
            {1, Color.fromKeyword(ColorKeyword.IndianRed) },
            {2, Color.fromKeyword(ColorKeyword.MediumSpringGreen) },
            {3, Color.fromKeyword(ColorKeyword.Orchid) },
            {4, Color.fromKeyword(ColorKeyword.Orange) },
            {5, Color.fromKeyword(ColorKeyword.Cyan) },
        };

        public static Dictionary<int, StyleParam.DrawingStyle> IntegerToLineDict = new()
        {
            // missed mono
            { 0, StyleParam.DrawingStyle.Solid },
            { 1, StyleParam.DrawingStyle.Dash },
            { 2, StyleParam.DrawingStyle.Dot },
            { 3, StyleParam.DrawingStyle.DashDot },

            // tolerance
            { 10, StyleParam.DrawingStyle.Solid },
            { 20, StyleParam.DrawingStyle.Dash },
            { 50, StyleParam.DrawingStyle.Dot },
            { 100, StyleParam.DrawingStyle.DashDot },
        };

        // built in static constructor
        public static Dictionary<int, Dictionary<int, Color>> ModAndMissedMonoToColorDict;
        public static Dictionary<int, List<Color>> ModToColorSetDict;
    }
}

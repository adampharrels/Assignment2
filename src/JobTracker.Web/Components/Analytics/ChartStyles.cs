using ApexCharts;

namespace JobTracker.Web.Components.Analytics;

/// <summary>
/// Shared look for every analytics chart: one validated colour set, thin rounded bars, a hairline
/// grid and no toolbar. Keeping it in one place means every chart on the page looks consistent.
/// </summary>
public static class ChartStyles
{
    /// <summary>Main series colour (and "you have this skill", "raises your chance").</summary>
    public const string Blue = "#2a78d6";

    /// <summary>Second series colour (and "you are missing this skill").</summary>
    public const string Orange = "#eb6834";

    /// <summary>Negative direction in the model-weights chart ("lowers your chance").</summary>
    public const string Red = "#e34948";

    /// <summary>Groups too small to trust are drawn in a recessive grey.</summary>
    public const string Muted = "#b5b4ad";

    private const string AxisText = "#52514e";
    private const string GridLine = "#e4e3df";

    /// <summary>Light-to-dark blue steps for the funnel stages (ordered, so one hue getting darker).</summary>
    public static readonly string[] FunnelRamp = { "#86b6ef", "#5598e7", "#2a78d6", "#1c5cab", "#104281" };

    /// <summary>JavaScript label formatter that adds a % sign; runs in the browser, so no round trip per label.</summary>
    private const string PercentFormatter = "function (value) { return Math.round(value) + '%'; }";

    /// <summary>
    /// Base options for a percentage bar chart. The value axis is the y axis for column charts and
    /// the x axis for horizontal bar charts.
    /// </summary>
    public static ApexChartOptions<TItem> CreatePercent<TItem>(bool horizontal = false) where TItem : class
    {
        ApexChartOptions<TItem> options = Create<TItem>(horizontal);
        if (horizontal)
        {
            options.Xaxis = new XAxis { Labels = new XAxisLabels { Formatter = PercentFormatter } };
        }
        else
        {
            // Category names sit on the x axis: rotate rather than hide them when they collide.
            options.Xaxis = new XAxis { Labels = new XAxisLabels { Rotate = -45, HideOverlappingLabels = false, Trim = true } };
            options.Yaxis = new List<YAxis> { new YAxis { Min = 0, Max = 100, Labels = new YAxisLabels { Formatter = PercentFormatter } } };
        }

        return options;
    }

    /// <summary>Base options for a bar, column or line chart.</summary>
    public static ApexChartOptions<TItem> Create<TItem>(bool horizontal = false) where TItem : class
    {
        return new ApexChartOptions<TItem>
        {
            Chart = new Chart
            {
                Toolbar = new Toolbar { Show = false },
                FontFamily = "Roboto, Helvetica, Arial, sans-serif",
                ForeColor = AxisText,
                Background = "transparent"
            },
            Grid = new Grid { BorderColor = GridLine },
            PlotOptions = new PlotOptions
            {
                Bar = new PlotOptionsBar
                {
                    Horizontal = horizontal,
                    BorderRadius = 4,
                    BorderRadiusApplication = BorderRadiusApplication.End,
                    BarHeight = "70%",
                    ColumnWidth = "60%"
                }
            },
            Legend = new Legend { Position = LegendPosition.Top, HorizontalAlign = Align.Left },
            DataLabels = new DataLabels { Enabled = false }
        };
    }
}

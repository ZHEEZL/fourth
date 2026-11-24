using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using SortingLab.Core.Visual;

namespace SortingLab.App.Controls;

/// <summary>Рисует массив столбиками и подсвечивает элементы текущего шага.</summary>
public sealed class BarChart : Control
{
    public static readonly StyledProperty<ChartFrame?> FrameProperty =
        AvaloniaProperty.Register<BarChart, ChartFrame?>(nameof(Frame));

    // Цвета как в тетради: обычные числа серые, как карандаш, а цвет получает только действие.
    // Синий — сравнение (ручка ученика), красный — перестановка, зелёный — число на своём месте.
    // Легенда под графиком и пояснение к шагу берут эти же кисти.
    public static readonly IImmutableBrush NormalBrush = Solid("#8A8A93");
    public static readonly IImmutableBrush FadedBrush = Solid("#E4E4E7");
    public static readonly IImmutableBrush CompareBrush = Solid("#2A52BE");
    public static readonly IImmutableBrush MoveBrush = Solid("#DC2626");
    public static readonly IImmutableBrush PivotBrush = Solid("#27272A");
    public static readonly IImmutableBrush SortedBrush = Solid("#16A34A");
    public static readonly IImmutableBrush HeldBrush = Solid("#262A52BE");

    private static readonly IImmutableBrush TextBrush = Solid("#171717");
    private static readonly IImmutableBrush IndexBrush = Solid("#737373");
    private static readonly IImmutableBrush HeldTextBrush = CompareBrush;
    private static readonly ImmutablePen HeldOutline = new(CompareBrush, 2, new ImmutableDashStyle([3, 2], 0));
    private static readonly ImmutablePen CompareOutline = new(CompareBrush, 3);

    // Тетрадная клетка под столбиками и линия, на которой они стоят.
    private const double CellSize = 20;
    private static readonly ImmutablePen GridPen = new(Solid("#0F171717"), 1);
    private static readonly ImmutablePen BaselinePen = new(Solid("#33171717"), 1);

    // Тот же шрифт, что и в остальном интерфейсе (UiFontFamily в App.axaml).
    private static readonly FontFamily ChartFont = new("avares://SortingLab.App/Assets/Fonts#Roboto");
    private static readonly Typeface ValueTypeface = new(ChartFont, FontStyle.Normal, FontWeight.SemiBold);
    private static readonly Typeface IndexTypeface = new(ChartFont);

    static BarChart()
    {
        AffectsRender<BarChart>(FrameProperty);
    }

    /// <summary>Кадр, который надо нарисовать.</summary>
    public ChartFrame? Frame
    {
        get => GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var area = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.White, area);

        var frame = Frame;
        if (frame is null || frame.Values.Length == 0)
            return;

        // Сверху место под подписи значений, снизу под номера позиций.
        const double topMargin = 30, bottomMargin = 22, sideMargin = 10;
        double plotWidth = area.Width - 2 * sideMargin;
        double plotHeight = area.Height - topMargin - bottomMargin;
        if (plotWidth <= 0 || plotHeight <= 0)
            return;

        int n = frame.Values.Length;
        int max = Math.Max(1, frame.Values.Max());
        if (frame.Held is int heldValue)
            max = Math.Max(max, heldValue);

        double slot = plotWidth / n;
        double barWidth = Math.Max(2, slot * 0.72);
        double valueFont = Math.Clamp(slot * 0.4, 9, 18);
        double indexFont = Math.Clamp(slot * 0.3, 8, 12);
        double baseline = topMargin + plotHeight;
        DrawGrid(context, area, baseline);

        for (int i = 0; i < n; i++)
        {
            // На свободном месте рисуется полупрозрачный столбик числа «в руке».
            bool isHole = i == frame.Hole && frame.Held is not null;
            int value = isHole ? frame.Held!.Value : frame.Values[i];

            double barHeight = Math.Max(3, plotHeight * value / max);
            double x = sideMargin + i * slot + (slot - barWidth) / 2;
            var bar = new Rect(x, baseline - barHeight, barWidth, barHeight);

            bool active = i == frame.First || i == frame.Second;
            bool compared = active && frame.Kind == StepKind.Compare;
            bool moved = active && (frame.Kind is StepKind.Swap or StepKind.Move);

            if (isHole)
            {
                context.DrawRectangle(HeldBrush, compared ? CompareOutline : HeldOutline, bar, 4, 4);
            }
            else
            {
                IBrush fill = moved ? MoveBrush
                    : i == frame.Pivot ? PivotBrush
                    : compared ? CompareBrush
                    : frame.Sorted.Contains(i) ? SortedBrush
                    : IsOutsideRange(frame, i) ? FadedBrush
                    : NormalBrush;

                // Опорный элемент в сравнении остаётся фиолетовым и получает жёлтую рамку.
                IPen? outline = compared && i == frame.Pivot ? CompareOutline : null;
                context.DrawRectangle(fill, outline, bar, 4, 4);
            }

            var valueText = MakeText(value.ToString(CultureInfo.InvariantCulture), ValueTypeface, valueFont,
                isHole ? HeldTextBrush : TextBrush);
            context.DrawText(valueText, new Point(x + (barWidth - valueText.Width) / 2, bar.Top - valueText.Height - 2));

            var indexText = MakeText(i.ToString(CultureInfo.InvariantCulture), IndexTypeface, indexFont, IndexBrush);
            context.DrawText(indexText, new Point(x + (barWidth - indexText.Width) / 2, baseline + 3));
        }
    }

    /// <summary>Рисует клетку с шагом <see cref="CellSize"/>, отсчитывая горизонтальные линии от основания.</summary>
    private static void DrawGrid(DrawingContext context, Rect area, double baseline)
    {
        // Смещение на полпикселя делает однопиксельные линии чёткими.
        for (double y = baseline % CellSize; y < area.Height; y += CellSize)
        {
            if (Math.Abs(y - baseline) < 1)
                continue; // основание рисуется отдельно, темнее
            double py = Math.Round(y) + 0.5;
            context.DrawLine(GridPen, new Point(0, py), new Point(area.Width, py));
        }
        for (double x = CellSize; x < area.Width; x += CellSize)
        {
            double px = Math.Round(x) + 0.5;
            context.DrawLine(GridPen, new Point(px, 0), new Point(px, area.Height));
        }

        double pb = Math.Round(baseline) + 0.5;
        context.DrawLine(BaselinePen, new Point(0, pb), new Point(area.Width, pb));
    }

    private static bool IsOutsideRange(ChartFrame frame, int i) =>
        frame.RangeStart >= 0 && (i < frame.RangeStart || i > frame.RangeEnd);

    private static FormattedText MakeText(string text, Typeface typeface, double size, IBrush brush) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, brush);

    private static IImmutableBrush Solid(string color) => new ImmutableSolidColorBrush(Color.Parse(color));
}

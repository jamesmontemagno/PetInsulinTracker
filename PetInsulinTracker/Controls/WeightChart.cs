using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PetInsulinTracker.Controls;

public class WeightChart : SKCanvasView
{
    public static readonly BindableProperty DataPointsProperty =
        BindableProperty.Create(nameof(DataPoints), typeof(IList<double>), typeof(WeightChart), null, propertyChanged: OnDataChanged);

    public static readonly BindableProperty LabelsProperty =
        BindableProperty.Create(nameof(Labels), typeof(IList<string>), typeof(WeightChart), null, propertyChanged: OnDataChanged);

    public static readonly BindableProperty LineColorProperty =
        BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(WeightChart), Color.FromArgb("#E8910C"), propertyChanged: OnDataChanged);

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(WeightChart), Color.FromArgb("#33E8910C"), propertyChanged: OnDataChanged);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(WeightChart), Color.FromArgb("#8C7B6B"), propertyChanged: OnDataChanged);

    public IList<double> DataPoints
    {
        get => (IList<double>)GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }

    public IList<string> Labels
    {
        get => (IList<string>)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((WeightChart)bindable).InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear();

        var data = DataPoints;
        if (data == null || data.Count < 2) return;

        var scale = (float)(info.Width / Width);
        float paddingLeft = 48 * scale;
        float paddingRight = 16 * scale;
        float paddingTop = 16 * scale;
        float paddingBottom = 32 * scale;

        float chartWidth = info.Width - paddingLeft - paddingRight;
        float chartHeight = info.Height - paddingTop - paddingBottom;

        double minVal = data.Min() * 0.9;
        double maxVal = data.Max() * 1.1;
        double range = maxVal - minVal;
        if (range < 0.01) range = 1;

        var points = new SKPoint[data.Count];
        for (int i = 0; i < data.Count; i++)
        {
            float x = paddingLeft + (i * chartWidth / (data.Count - 1));
            float y = paddingTop + chartHeight - (float)((data[i] - minVal) / range * chartHeight);
            points[i] = new SKPoint(x, y);
        }

        // Grid lines
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(200, 190, 175, 60),
            StrokeWidth = 1 * scale,
            Style = SKPaintStyle.Stroke
        };
        for (int i = 0; i <= 4; i++)
        {
            float y = paddingTop + (i * chartHeight / 4);
            canvas.DrawLine(paddingLeft, y, info.Width - paddingRight, y, gridPaint);
        }

        // Y-axis labels
        using var labelFont = new SKFont
        {
            Size = 10 * scale
        };
        using var labelPaint = new SKPaint
        {
            Color = ToSkColor(TextColor),
            IsAntialias = true
        };
        for (int i = 0; i <= 4; i++)
        {
            float y = paddingTop + (i * chartHeight / 4);
            double val = maxVal - (i * range / 4);
            canvas.DrawText(val.ToString("F1"), paddingLeft - 6 * scale, y + 4 * scale, SKTextAlign.Right, labelFont, labelPaint);
        }

        // Gradient fill under line
        using var fillPath = new SKPath();
        fillPath.MoveTo(points[0].X, paddingTop + chartHeight);
        foreach (var p in points)
            fillPath.LineTo(p.X, p.Y);
        fillPath.LineTo(points[^1].X, paddingTop + chartHeight);
        fillPath.Close();

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, paddingTop),
                new SKPoint(0, paddingTop + chartHeight),
                [ToSkColor(LineColor).WithAlpha(80), ToSkColor(LineColor).WithAlpha(5)],
                [0f, 1f],
                SKShaderTileMode.Clamp)
        };
        canvas.DrawPath(fillPath, fillPaint);

        // Smooth line
        using var linePath = new SKPath();
        linePath.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];
            float cx = (prev.X + curr.X) / 2;
            linePath.CubicTo(cx, prev.Y, cx, curr.Y, curr.X, curr.Y);
        }

        using var linePaint = new SKPaint
        {
            Color = ToSkColor(LineColor),
            StrokeWidth = 3 * scale,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawPath(linePath, linePaint);

        // Data points
        using var dotPaint = new SKPaint
        {
            Color = ToSkColor(LineColor),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var dotBorderPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        foreach (var p in points)
        {
            canvas.DrawCircle(p, 5 * scale, dotBorderPaint);
            canvas.DrawCircle(p, 3.5f * scale, dotPaint);
        }

        // X-axis labels
        var labels = Labels;
        if (labels != null)
        {
            using var xLabelFont = new SKFont
            {
                Size = 9 * scale
            };
            using var xLabelPaint = new SKPaint
            {
                Color = ToSkColor(TextColor),
                IsAntialias = true
            };
            int step = Math.Max(1, labels.Count / 6);
            for (int i = 0; i < labels.Count; i += step)
            {
                if (i < points.Length)
                    canvas.DrawText(labels[i], points[i].X, info.Height - 4 * scale, SKTextAlign.Center, xLabelFont, xLabelPaint);
            }
        }
    }

    static SKColor ToSkColor(Color color) =>
        new((byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255), (byte)(color.Alpha * 255));
}

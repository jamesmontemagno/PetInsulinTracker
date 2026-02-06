using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PetInsulinTracker.Controls;

public class DoseIndicator : SKCanvasView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(DoseIndicator), 0.0, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty RingColorProperty =
        BindableProperty.Create(nameof(RingColor), typeof(Color), typeof(DoseIndicator), Color.FromArgb("#E8910C"), propertyChanged: OnVisualChanged);

    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(DoseIndicator), Color.FromArgb("#E8DDD0"), propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CenterTextProperty =
        BindableProperty.Create(nameof(CenterText), typeof(string), typeof(DoseIndicator), string.Empty, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty SubTextProperty =
        BindableProperty.Create(nameof(SubText), typeof(string), typeof(DoseIndicator), string.Empty, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CenterTextColorProperty =
        BindableProperty.Create(nameof(CenterTextColor), typeof(Color), typeof(DoseIndicator), Color.FromArgb("#3D2C1E"), propertyChanged: OnVisualChanged);

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color RingColor
    {
        get => (Color)GetValue(RingColorProperty);
        set => SetValue(RingColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public string SubText
    {
        get => (string)GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public Color CenterTextColor
    {
        get => (Color)GetValue(CenterTextColorProperty);
        set => SetValue(CenterTextColorProperty, value);
    }

    static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((DoseIndicator)bindable).InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear();

        float size = Math.Min(info.Width, info.Height);
        float scale = (float)(info.Width / Width);
        float strokeWidth = 10 * scale;
        float radius = (size - strokeWidth) / 2;
        var center = new SKPoint(info.Width / 2f, info.Height / 2f);

        // Track ring
        using var trackPaint = new SKPaint
        {
            Color = ToSkColor(TrackColor),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawCircle(center, radius, trackPaint);

        // Progress arc
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress > 0)
        {
            var sweepAngle = (float)(progress * 360);
            var rect = new SKRect(
                center.X - radius, center.Y - radius,
                center.X + radius, center.Y + radius);

            using var progressPaint = new SKPaint
            {
                StrokeWidth = strokeWidth,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                Shader = SKShader.CreateSweepGradient(
                    center,
                    [ToSkColor(RingColor).WithAlpha(180), ToSkColor(RingColor)],
                    [0f, (float)progress])
            };

            using var path = new SKPath();
            path.AddArc(rect, -90, sweepAngle);
            canvas.DrawPath(path, progressPaint);

            // Dot at end of progress
            float endAngle = -90 + sweepAngle;
            float endRad = endAngle * MathF.PI / 180;
            var dotPoint = new SKPoint(
                center.X + radius * MathF.Cos(endRad),
                center.Y + radius * MathF.Sin(endRad));

            using var dotPaint = new SKPaint
            {
                Color = ToSkColor(RingColor),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(dotPoint, strokeWidth * 0.7f, dotPaint);

            using var dotInnerPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(dotPoint, strokeWidth * 0.3f, dotInnerPaint);
        }

        // Center text
        if (!string.IsNullOrEmpty(CenterText))
        {
            using var textFont = new SKFont
            {
                Size = 22 * scale,
                Embolden = true
            };
            using var textPaint = new SKPaint
            {
                Color = ToSkColor(CenterTextColor),
                IsAntialias = true
            };
            canvas.DrawText(CenterText, center.X, center.Y + 8 * scale, SKTextAlign.Center, textFont, textPaint);
        }

        // Sub text
        if (!string.IsNullOrEmpty(SubText))
        {
            using var subFont = new SKFont
            {
                Size = 10 * scale
            };
            using var subPaint = new SKPaint
            {
                Color = ToSkColor(CenterTextColor).WithAlpha(150),
                IsAntialias = true
            };
            canvas.DrawText(SubText, center.X, center.Y + 24 * scale, SKTextAlign.Center, subFont, subPaint);
        }
    }

    static SKColor ToSkColor(Color color) =>
        new((byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255), (byte)(color.Alpha * 255));
}

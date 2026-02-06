using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PetInsulinTracker.Controls;

public class GradientCard : SKCanvasView
{
    public static readonly BindableProperty StartColorProperty =
        BindableProperty.Create(nameof(StartColor), typeof(Color), typeof(GradientCard), Color.FromArgb("#E8910C"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty EndColorProperty =
        BindableProperty.Create(nameof(EndColor), typeof(Color), typeof(GradientCard), Color.FromArgb("#F28B6E"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty CardCornerRadiusProperty =
        BindableProperty.Create(nameof(CardCornerRadius), typeof(float), typeof(GradientCard), 16f, propertyChanged: OnVisualPropertyChanged);

    public Color StartColor
    {
        get => (Color)GetValue(StartColorProperty);
        set => SetValue(StartColorProperty, value);
    }

    public Color EndColor
    {
        get => (Color)GetValue(EndColorProperty);
        set => SetValue(EndColorProperty, value);
    }

    public float CardCornerRadius
    {
        get => (float)GetValue(CardCornerRadiusProperty);
        set => SetValue(CardCornerRadiusProperty, value);
    }

    static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((GradientCard)bindable).InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear();

        var scale = (float)(info.Width / Width);
        var cornerRadius = CardCornerRadius * scale;

        using var rect = new SKRoundRect(new SKRect(0, 0, info.Width, info.Height), cornerRadius);

        // Gradient fill
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(info.Width, info.Height),
                [ToSkColor(StartColor), ToSkColor(EndColor)],
                [0f, 1f],
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRoundRect(rect, paint);

        // Subtle inner highlight
        using var highlightPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, 25),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f * scale
        };
        var insetRect = new SKRoundRect(
            new SKRect(1 * scale, 1 * scale, info.Width - 1 * scale, info.Height - 1 * scale),
            cornerRadius - 1 * scale);
        canvas.DrawRoundRect(insetRect, highlightPaint);
    }

    static SKColor ToSkColor(Color color) =>
        new((byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255), (byte)(color.Alpha * 255));
}

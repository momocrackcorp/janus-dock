using System.Drawing;

namespace JanusDock;

/// <summary>
/// Shared WCAG-style luminance logic for controls painted directly over the
/// user's custom Dock color. Windows light/dark preference is insufficient
/// because a custom Dock color may be opposite to the system theme.
/// </summary>
internal static class DockContrast
{
    private static double Channel(byte value)
    {
        double c = value / 255d;
        return c <= .04045d ? c / 12.92d : Math.Pow((c + .055d) / 1.055d, 2.4d);
    }

    private static double Luminance(Color color) =>
        .2126d * Channel(color.R) + .7152d * Channel(color.G)
        + .0722d * Channel(color.B);

    public static bool IsLight(Color background) => Luminance(background) >= .19d;

    public static Color Foreground(Color background) => IsLight(background)
        ? Color.FromArgb(28, 33, 42) : Color.FromArgb(244, 246, 249);

    public static Color Secondary(Color background) => IsLight(background)
        ? Color.FromArgb(73, 82, 98) : Color.FromArgb(207, 215, 226);

    public static Color Blue(Color background) => IsLight(background)
        ? Color.FromArgb(13, 93, 164) : Color.FromArgb(95, 203, 244);

    public static Color Green(Color background) => IsLight(background)
        ? Color.FromArgb(8, 111, 50) : Color.FromArgb(91, 229, 134);

    public static Color Orange(Color background) => IsLight(background)
        ? Color.FromArgb(163, 70, 0) : Color.FromArgb(255, 176, 91);
}

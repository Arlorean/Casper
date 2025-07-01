using System;

using Spectre.Console;

using static Spectre.Console.Color;

namespace Casper.Console;

public static class Colors {
    public static Style GetStyle(byte attr) {
        var foreground = ArcaneUtil.GetInk16(attr);
        var background = ArcaneUtil.GetPaper16(attr);
        return new Style(FromColorIndex16(foreground), FromColorIndex16(background));
    }
    static Color FromColorIndex16(ColorIndex16 i) {
        return Palette16[(int)i];
    }

    static readonly Color[] Palette16 = {
        // Normal colors
        Black, Navy, Green, Teal,
        Maroon, Purple, Olive, Grey,

        // Bright colors
        Black, Blue, Lime, Aqua,
        Red, Fuchsia, Yellow, White,
    };
}

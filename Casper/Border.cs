using System;
using System.Drawing;

namespace Casper;

public class Border {
    ColorIndex16 border = ColorIndex16.Black;

    public const int PixelsLeft = 48;
    public const int PixelsRight = 48;
    public const int PixelsTop = 48;
    public const int PixelsBottom = 56;

    public static readonly Rectangle Rectangle = new Rectangle(
        0, 0,
        Screen.PixelsWide, Screen.PixelsHigh
    );
    public static readonly Rectangle InnerRectangle = new Rectangle(
        PixelsLeft, PixelsTop,
        Screen.PixelsWide, Screen.PixelsHigh
    );
    public static readonly Rectangle OuterRectangle = new Rectangle(
        0, 0,
        PixelsLeft + Screen.PixelsWide + PixelsRight,
        PixelsTop + Screen.PixelsHigh + PixelsBottom
    );

    public event Action<ColorIndex16> RenderBorder;

    public void SetBorder(ColorIndex16 value) {
        if (border != value) {
            border = value;
            RenderBorder?.Invoke(border);
        }
    }
}

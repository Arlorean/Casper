using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Casper.Console;

public struct Octant {
    public const int PixelsHigh = 4;
    public const int PixelsWide = 2;  

    bool[,] bits = new bool[PixelsWide, PixelsHigh];

    public Octant() {}

    public bool this[int x, int y] {
        get => bits[x,y];
        set => bits[x,y] = value;
    }

    public Rune ToRune() {
        var i = 0;
        var b = 1;
        for (var y = 0; y < PixelsHigh; y++) {
            for (var x = 0; x < PixelsWide; x++) {
                if (bits[x, y]) {
                    i |= b;
                }
                b <<= 1;
            }
        }
        return Runes[i];
    }

    static Rune[]? utf32;
    
    static Rune[] Runes {
        get {
            if (utf32 == null) {
                var bytes = Resources.Get(Shared.Resources.Assembly, @"Resources\Runes\Octants.txt");
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                utf32 = reader.ReadToEnd().ReplaceLineEndings("").EnumerateRunes().ToArray();
            }
            return utf32;
        }
    }
}

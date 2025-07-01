
namespace Casper;

public static class ArcaneUtil {
    public static (int cx, int py) GetPixelCoordinates(int address) {
        // http://www.zxdesign.info/memoryToScreen.shtml
        var cx = address & 0b000_00_000_000_11111;
        var py = ((address & 0b000_11_000_000_00000) >> 5)
                | ((address & 0b000_00_000_111_00000) >> 2)
                | ((address & 0b000_00_111_000_00000) >> 8);
        return (cx, py);
    }

    public static (int cx, int cy) GetAttrCoordinates(int address) {
        var cx = (address & 0b000000_00000_11111) >> 0;
        var cy = (address & 0b000000_11111_00000) >> 5;
        return (cx, cy);
    }

    public static bool IsFlash(byte attr) =>
        (attr & 0b_1_0_000_000) != 0;
    public static bool IsBright(byte attr) =>
        (attr & 0b_0_1_000_000) != 0;
    public static ColorIndex16 GetInk16(byte attr) => (ColorIndex16)
        ((attr & 0b_0_0_000_111) >> 0 | (IsBright(attr) ? 8 : 0));
    public static ColorIndex16 GetPaper16(byte attr) => (ColorIndex16)
        ((attr & 0b_0_0_111_000) >> 3 | (IsBright(attr) ? 8 : 0));
}

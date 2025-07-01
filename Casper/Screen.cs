using System;

namespace Casper {
    /// <summary>
    /// Convert byte updates in memory to pixel updates on the screen.
    /// http://www.zxdesign.info/memoryToScreen.shtml
    /// </summary>
    public class Screen {
        readonly byte[,] pixels = new byte[AttrsWide, PixelsHigh];
        readonly byte[,] attrs = new byte[AttrsWide, AttrsHigh];
        bool flashInverted;
        int flashCounter;

        // http://www.zxdesign.info/vidparam.shtml
        // https://www.worldofspectrum.org/faq/reference/48kreference.htm#ZXSpectrum
        public const int PixelsWide = 256;
        public const int PixelsHigh = 192;
        public const int AttrsWide = PixelsWide / 8;
        public const int AttrsHigh = PixelsHigh / 8;


        public Screen(Spectrum spectrum) {
            spectrum.Interrupt += UpdateFlash;
        }

        public byte GetByte(int xChar, int yPixel) { return pixels[xChar, yPixel]; }
        public byte GetAttr(int xChar, int yChar) { return attrs[xChar, yChar]; }

        public event Action<int, int, ColorIndex16> RenderPixel;

        void UpdateFlash() {
            // Characters flash every 16 frames (16/50s = 0.32s)
            // https://www.worldofspectrum.org/faq/reference/48kreference.htm#ZXSpectrum
            if ((flashCounter++ % 16) == 0) {
                Flash();
            }
        }

        void Flash() {
            flashInverted = !flashInverted;

            if (RenderPixel == null) { return; }

            for (var cy = 0; cy < 24; cy++) {
                for (var cx = 0; cx < 32; ++cx) {
                    var attr = attrs[cx, cy];
                    if (attr.IsBit7Set()) {
                        RenderAttr(cx, cy, attr);
                    }
                }
            }
        }

        public void SetMemory(int address, byte value) {
            if (RenderPixel == null) { return; }

            if (address < (16384+(32*192))) {
                UpdatePixels(address, value);
            }
            else {
                UpdateColors(address, value);
            }
        }


        void UpdatePixels(int address, byte newValue) {
            var (cx, py) = ArcaneUtil.GetPixelCoordinates(address);
            var cy = py >> 3;

            var oldValue = pixels[cx, py];
            pixels[cx, py] = newValue;

            var attr = attrs[cx, cy];
            var foreground = ArcaneUtil.GetInk16(attr);
            var background = ArcaneUtil.GetPaper16(attr);
            if (attr.IsBit7Set() && flashInverted) {
                (foreground, background) = (background, foreground);
            }

            var px = cx << 3;
            var changes = (byte) (newValue ^ oldValue);
            for (var dx = 7; dx >= 0; --dx) {
                if (changes.IsBit0Set()) {
                    var isForeground = newValue.IsBit0Set();
                    var index = isForeground ? foreground : background;
                    RenderPixel(px+dx, py, index);
                }
                changes >>= 1;
                newValue >>= 1;
            }
        }

        void UpdateColors(int address, byte attr) {
            var (cx, cy) = ArcaneUtil.GetAttrCoordinates(address);
            attrs[cx, cy] = attr;

            RenderAttr(cx, cy, attr);
        }

        void RenderAttr(int cx, int cy, byte attr) {
            var foreground = ArcaneUtil.GetInk16(attr);
            var background = ArcaneUtil.GetPaper16(attr);
            if (ArcaneUtil.IsFlash(attr) && flashInverted) {
                (foreground, background) = (background, foreground);
            }

            var px = cx << 3;
            var py = cy << 3;
            for (var dy = 0; dy < 8; dy++) {
                var value = pixels[cx, py + dy];
                for (var dx = 7; dx >= 0; --dx) {
                    var isForeground = value.IsBit0Set();
                    RenderPixel(px + dx, py + dy, isForeground ? foreground : background);
                    value >>= 1;
                }
            }
        }
    }
}

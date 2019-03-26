/*
 * @(#)Spectrum.java 1.1 27/04/97 Adam Davidson & Andrew Pollard
 */
using System;
using System.Drawing;
using System.IO;
using System.Reflection;



namespace Casper {

    /// <summary>
    /// The Spectrum class extends the Z80 class implementing the supporting
    /// hardware emulation which was specific to the ZX Spectrum. This
    /// includes the memory mapped screen and the IO ports which were used
    /// to read the keyboard, change the border color and turn the speaker
    /// on/off. There is no sound support in this version.
    /// </summary>
    public class Spectrum : Z80 {
        IDisplay display;

        public Spectrum(IDisplay display) : base(3.5) { // Spectrum runs at 3.5Mhz
            this.display = display;

            resetKeyboard();
        }

        public void setBorderWidth(int width) {
            borderWidth = width;
        }

        /// <summary>
        /// Z80 hardware interface
        /// </summary>
        public override int inb(int port) {
            int res = 0xff;

            if ((port & 0x0001) == 0) {
                if ((port & 0x8000) == 0) { res &= banks[7]; }
                if ((port & 0x4000) == 0) { res &= banks[6]; }
                if ((port & 0x2000) == 0) { res &= banks[5]; }
                if ((port & 0x1000) == 0) { res &= banks[4]; }
                if ((port & 0x0800) == 0) { res &= banks[3]; }
                if ((port & 0x0400) == 0) { res &= banks[2]; }
                if ((port & 0x0200) == 0) { res &= banks[1]; }
                if ((port & 0x0100) == 0) { res &= banks[0]; }
            }

            return (res);
        }
        public override void outb(int port, int outByte, int tstates) {
            if ((port & 0x0001) == 0) {
                newBorder = (outByte & 0x07);
            }
        }

        /** Byte access */
        public override void pokeb(int addr, int newByte) {
            if (addr >= (22528 + 768)) {
                mem[addr] = newByte;
                return;
            }

            if (addr < 16384) {
                return;
            }

            if (mem[addr] != newByte) {
                plot(addr, newByte);
            }
        }

        // Word access
        public override void pokew(int addr, int word) {
            if (addr >= (22528 + 768)) {
                mem[addr] = word & 0xff;
                if (++addr != 65536) {
                    mem[addr] = word >> 8;
                }
                return;
            }

            if (addr < 16384) {
                return;
            }

            int newByte0 = word & 0xff;
            if (mem[addr] != newByte0) {
                plot(addr, newByte0);
                mem[addr] = newByte0;
            }

            int newByte1 = word >> 8;
            if (++addr != (22528 + 768)) {
                if (mem[addr] != newByte1) {
                    plot(addr, newByte1);
                    mem[addr] = newByte1;
                }
            }
            else {
                mem[addr] = newByte1;
            }
        }

        /** Since execute runs as a tight loop, some Java VM implementations
         *  don't allow any other threads to get a look in. This give the
         *  GUI time to update. If anyone has a better solution please 
         *  email us at mailto:spectrum@odie.demon.co.uk
         */
        public int sleepHack = 0;
        public int refreshRate = 1;  // refresh every 'n' interrupts

        private int interruptCounter = 0;
        private bool resetAtNextInterrupt = false;
        private bool pauseAtNextInterrupt = false;
        private bool refreshNextInterrupt = true;
        private bool loadFromURLFieldNextInterrupt = false;

        public long timeOfLastInterrupt = 0;
        private long timeOfLastSample = 0;

        public override int interrupt() {
            interruptCounter++;

            // Characters flash every 1/2 a second
            if ((interruptCounter % 25) == 0) {
                refreshFlashChars();
            }

            // Refresh every interrupt by default
            if ((interruptCounter % refreshRate) == 0) {
                //screenPaint();
            }


            return base.interrupt();
        }

        public override void reset() {
            base.reset();

            outb(254, 0xff, 0); // White border on startup
        }

        /**
         * Screen stuff
         */
        public int borderWidth = 20;   // absolute, not relative to pixelScale

        public const int nPixelsWide = 256;
        public const int nPixelsHigh = 192;
        public const int nCharsWide = 32;
        public const int nCharsHigh = 24;

        private const int sat = 238;
        private static readonly Color[] brightColors = {
            Color.FromArgb(   0,   0,   0 ),  Color.FromArgb(   0,   0, sat ),
            Color.FromArgb( sat,   0,   0 ),  Color.FromArgb( sat,   0, sat ),
            Color.FromArgb(   0, sat,   0 ),  Color.FromArgb(   0, sat, sat ),
            Color.FromArgb( sat, sat,   0 ),  Color.FromArgb( sat, sat, sat ),
            Color.Black,                 Color.Blue,
            Color.Red,                   Color.Magenta,
            Color.Green,                 Color.Cyan,
            Color.Yellow,                Color.White
        };
        private const int firstAttr = (nPixelsHigh * nCharsWide);
        private const int lastAttr = firstAttr + (nCharsHigh * nCharsWide);

        public int newBorder = 7;  // White border on startup
        public int oldBorder = -1; // -1 mean update screen

        public long oldTime = 0;
        public int oldSpeed = -1; // -1 mean update progressBar
        public int newSpeed = 0;
        public bool showStats = true;

        private bool flashInvert = false;

        private void refreshFlashChars() {
            flashInvert = !flashInvert;

            for (int i = firstAttr; i < lastAttr; i++) {
                int attr = mem[i + 16384];

                if ((attr & 0x80) != 0) {
                    //last[i] = (~attr) & 0xff;

                    //// Only add to update list if not already marked 
                    //if ( next[i] == -1 ) {
                    //	next[i] = FIRST;
                    //	FIRST = i;
                    //}
                }
            }
        }

        private void plot(int addr, int newByte) {
            mem[addr] = newByte;
            PokeScreenByte(addr, (byte)newByte);
        }

        public void PokeScreenByte(int address, byte value) {
            var i = address - 16384;
            if (i < 6144) {
                DrawScreenByte(address, value);
            }
            else {
                DrawScreenAttr(address, value);
            }
        }

        void DrawScreenAttr(int address, byte value) {
            var i = address - 16384 - 6144;
            var x = (i % 32);
            var y = (i / 32);
            var addr = (0b010 << 13)
                | ((y & 0b00111) << 5)
                | ((y & 0b11000) << 8)
                | (x);

            for (var b = 0; b < 8; b++) {
                DrawScreenByte(addr, (byte)mem[addr]);
                addr += 256; // Move to next pixel row
            }
        }

        void DrawScreenByte(int address, byte value) {
            // http://www.zxdesign.info/memoryToScreen.shtml
            var i = address - 16384;
            int x = ((i & 0x1f) << 3);
            int y = ((i & 0x00e0) >> 2)
                  + ((i & 0x0700) >> 8)
                  + ((i & 0x1800) >> 5);

            // https://faqwiki.zxnet.co.uk/wiki/Spectrum_Video_Modes
            int j = 16384 + 6144
                + (i & 0xff)
                + ((i & 0x1800) >> 3);
            var attr = (byte)mem[j];

            for (var b = 0; b < 8; b++) {
                var pixel = ((value >> b) & 1) == 1;
                display.RenderPixel(x + (7 - b), y, pixel, attr);
            }
        }

        public void borderPaint() {
            if (oldBorder == newBorder) {
                return;
            }
            oldBorder = newBorder;

            if (borderWidth == 0) {
                return;
            }

            //parentGraphics.setColor( brightColors[ newBorder ] );
            //parentGraphics.fillRect( 0, 0,
            //	(nPixelsWide*pixelScale) + borderWidth*2,
            //	(nPixelsHigh*pixelScale) + borderWidth*2 );
        }

        public void resetKeyboard() {
            for (var i=0; i < 8; ++i) {
                banks[i] = 0xff;
            }
        }

        byte[] banks = new byte[8];

        public void DoKeys(bool down, params Key[] keys) {
            foreach (var key in keys) {
                var bank = ((int)key / 5);
                var bit = 1 << ((int)key % 5);
                if (down) {
                    banks[bank] &= (byte)(~bit);
                }
                else {
                    banks[bank] |= (byte)(bit);
                }
            }
        }

        public void RefreshWholeScreen() {
            var addr = 16384 + 6144;
            for (var i = 0; i < (32 * 24); ++i) {
                DrawScreenAttr(addr + i, (byte)mem[addr + i]);
            }
        }

        public void loadSnapshot(Stream stream, int snapshotLength) {
            // Crude check but it'll work (SNA is a fixed size)
            if ((snapshotLength == 49179)) {
                loadSNA(stream);
            }
            else {
                loadZ80(stream, snapshotLength);
            }

            RefreshWholeScreen();
            resetKeyboard();
        }

        public void loadROM(Stream stream) {
            readBytes(stream, mem, 0, 16384);
        }

        public void loadSNA(Stream stream) {
            int[] header = new int[27];

            readBytes(stream, header, 0, 27);
            readBytes(stream, mem, 16384, 49152);

            I(header[0]);

            HL(header[1] | (header[2] << 8));
            DE(header[3] | (header[4] << 8));
            BC(header[5] | (header[6] << 8));
            AF(header[7] | (header[8] << 8));

            exx();
            ex_af_af();

            HL(header[9] | (header[10] << 8));
            DE(header[11] | (header[12] << 8));
            BC(header[13] | (header[14] << 8));

            IY(header[15] | (header[16] << 8));
            IX(header[17] | (header[18] << 8));

            if ((header[19] & 0x04) != 0) {
                IFF2(true);
            }
            else {
                IFF2(false);
            }

            R(header[20]);

            AF(header[21] | (header[22] << 8));
            SP(header[23] | (header[24] << 8));

            switch (header[25]) {
            case 0:
                IM(IM0);
                break;
            case 1:
                IM(IM1);
                break;
            default:
                IM(IM2);
                break;
            }

            outb(254, header[26], 0); // border

            /* Emulate RETN to start */
            IFF1(IFF2());
            REFRESH(2);
            poppc();
        }

        public void loadZ80(Stream stream, int bytesLeft) {
            int[] header = new int[30];
            bool compressed = false;

            bytesLeft -= readBytes(stream, header, 0, 30);

            A(header[0]);
            F(header[1]);

            C(header[2]);
            B(header[3]);
            L(header[4]);
            H(header[5]);

            PC(header[6] | (header[7] << 8));
            SP(header[8] | (header[9] << 8));

            I(header[10]);
            R(header[11]);

            int tbyte = header[12];
            if (tbyte == 255) {
                tbyte = 1;
            }

            outb(254, ((tbyte >> 1) & 0x07), 0); // border

            if ((tbyte & 0x01) != 0) {
                R(R() | 0x80);
            }
            compressed = ((tbyte & 0x20) != 0);

            E(header[13]);
            D(header[14]);

            ex_af_af();
            exx();

            C(header[15]);
            B(header[16]);
            E(header[17]);
            D(header[18]);
            L(header[19]);
            H(header[20]);

            A(header[21]);
            F(header[22]);

            ex_af_af();
            exx();

            IY(header[23] | (header[24] << 8));
            IX(header[25] | (header[26] << 8));

            IFF1(header[27] != 0);
            IFF2(header[28] != 0);

            switch (header[29] & 0x03) {
            case 0:
                IM(IM0);
                break;
            case 1:
                IM(IM1);
                break;
            default:
                IM(IM2);
                break;
            }

            if (PC() == 0) {
                loadZ80_extended(stream, bytesLeft);
                return;
            }

            /* Old format Z80 snapshot */

            if (compressed) {
                int[] data = new int[bytesLeft];
                int addr = 16384;

                int size = readBytes(stream, data, 0, bytesLeft);
                int i = 0;

                while ((addr < 65536) && (i < size)) {
                    tbyte = data[i++];
                    if (tbyte != 0xed) {
                        pokeb(addr, tbyte);
                        addr++;
                    }
                    else {
                        tbyte = data[i++];
                        if (tbyte != 0xed) {
                            pokeb(addr, 0xed);
                            i--;
                            addr++;
                        }
                        else {
                            int count;
                            count = data[i++];
                            tbyte = data[i++];
                            while ((count--) != 0) {
                                pokeb(addr, tbyte);
                                addr++;
                            }
                        }
                    }
                }
            }
            else {
                readBytes(stream, mem, 16384, 49152);
            }
        }

        private void loadZ80_extended(Stream stream, int bytesLeft) {
            int[] header = new int[2];
            bytesLeft -= readBytes(stream, header, 0, header.Length);

            int type = header[0] | (header[1] << 8);

            switch (type) {
            case 23: /* V2.01 */
                loadZ80_v201(stream, bytesLeft);
                break;
            case 54: /* V3.00 */
                loadZ80_v300(stream, bytesLeft);
                break;
            case 58: /* V3.01 */
                loadZ80_v301(stream, bytesLeft);
                break;
            default:
                throw new Exception("Z80 (extended): unsupported type " + type);
            }
        }

        private void loadZ80_v201(Stream stream, int bytesLeft) {
            int[] header = new int[23];
            bytesLeft -= readBytes(stream, header, 0, header.Length);

            PC(header[0] | (header[1] << 8));

            /* 0 - 48K
             * 1 - 48K + IF1
             * 2 - SamRam
             * 3 - 128K
             * 4 - 128K + IF1
             */
            int type = header[2];

            if (type > 1) {
                throw new Exception("Z80 (v201): unsupported type " + type);
            }

            int[] data = new int[bytesLeft];
            readBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = loadZ80_page(data, offset);
            }
        }

        private void loadZ80_v300(Stream stream, int bytesLeft) {
            int[] header = new int[54];
            bytesLeft -= readBytes(stream, header, 0, header.Length);

            PC(header[0] | (header[1] << 8));

            /* 0 - 48K
             * 1 - 48K + IF1
             * 2 - 48K + MGT
             * 3 - SamRam
             * 4 - 128K
             * 5 - 128K + IF1
             * 6 - 128K + MGT
             */
            int type = header[2];

            if (type > 6) {
                throw new Exception("Z80 (v300): unsupported type " + type);
            }

            int[] data = new int[bytesLeft];
            readBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = loadZ80_page(data, offset);
            }
        }

        private void loadZ80_v301(Stream stream, int bytesLeft) {
            int[] header = new int[58];
            bytesLeft -= readBytes(stream, header, 0, header.Length);

            PC(header[0] | (header[1] << 8));

            /* 0 - 48K
             * 1 - 48K + IF1
             * 2 - 48K + MGT
             * 3 - SamRam
             * 4 - 128K
             * 5 - 128K + IF1
             * 6 - 128K + MGT
             * 7 - +3
             */
            int type = header[2];

            if (type > 7) {
                throw new Exception("Z80 (v301): unsupported type " + type);
            }

            int[] data = new int[bytesLeft];
            readBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = loadZ80_page(data, offset);
            }
        }

        private int loadZ80_page(int[] data, int i) {
            int blocklen;
            int page;

            blocklen = data[i++];
            blocklen |= (data[i++]) << 8;
            page = data[i++];

            int addr;
            switch (page) {
            case 4:
                addr = 32768;
                break;
            case 5:
                addr = 49152;
                break;
            case 8:
                addr = 16384;
                break;
            default:
                throw new Exception("Z80 (page): out of range " + page);
            }

            int k = 0;
            while (k < blocklen) {
                int tbyte = data[i++]; k++;
                if (tbyte != 0xed) {
                    pokeb(addr, ~tbyte);
                    pokeb(addr, tbyte);
                    addr++;
                }
                else {
                    tbyte = data[i++]; k++;
                    if (tbyte != 0xed) {
                        pokeb(addr, 0);
                        pokeb(addr, 0xed);
                        addr++;
                        i--; k--;
                    }
                    else {
                        int count;
                        count = data[i++]; k++;
                        tbyte = data[i++]; k++;
                        while (count-- > 0) {
                            pokeb(addr, ~tbyte);
                            pokeb(addr, tbyte);
                            addr++;
                        }
                    }
                }
            }

            if ((addr & 16383) != 0) {
                throw new Exception("Z80 (page): overrun");
            }

            return i;
        }


        public int bytesReadSoFar = 0;
        public int bytesToReadTotal = 0;

        private int readBytes(Stream stream, int[] a, int off, int n) {
            byte[] buff = new byte[n];
            int toRead = n;
            while (toRead > 0) {
                int nRead = stream.Read(buff, n - toRead, toRead);
                toRead -= nRead;
            }

            for (int i = 0; i < n; i++) {
                a[i + off] = (buff[i] + 256) & 0xff;
            }

            return n;
        }
    }
}
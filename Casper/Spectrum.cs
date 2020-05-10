/*
 * @(#)Spectrum.java 1.1 27/04/97 Adam Davidson & Andrew Pollard
 */
using System;
using System.Collections.Generic;
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
        public Screen Screen { get; } = new Screen();
        public Keyboard Keyboard { get; } = new Keyboard();

        public Spectrum() : base(3.5) { // Spectrum runs at 3.5Mhz
        }

        /// <summary>
        /// Z80 hardware interface
        /// </summary>
        public override int inb(int port) {
            if ((port & 0x0001) == 0) {
                return Keyboard.InPort((ushort)port);
            }
            return 0xff;
        }
        public override void outb(int port, int outByte, int tstates) {
            if ((port & 0x0001) == 0) {
                Screen.Border = (ColorIndex)(outByte & 0x07);
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
                Screen.UpdateByte(addr, (byte)newByte);
                mem[addr] = newByte;
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
                Screen.UpdateByte(addr, (byte)newByte0);
                mem[addr] = newByte0;
            }

            int newByte1 = word >> 8;
            if (++addr != (22528 + 768)) {
                if (mem[addr] != newByte1) {
                    Screen.UpdateByte(addr, (byte)newByte1);
                    mem[addr] = newByte1;
                }
            }
            else {
                mem[addr] = newByte1;
            }
        }

        private int interruptCounter = 0;

        public override int interrupt() {
            interruptCounter++;

            // Characters flash every 16 frames (16/50s = 0.32s)
            // https://www.worldofspectrum.org/faq/reference/48kreference.htm#ZXSpectrum
            if ((interruptCounter % 16) == 0) {
                Screen.Flash();
            }

            return base.interrupt();
        }

        public override void Step() {
            Keyboard.ProcessLogicalKeys();
            base.Step();
        }

        public override void Reset() {
            base.Reset();

            outb(254, 0xff, 0); // White border on startup
        }

        public int newBorder = 7;  // White border on startup
        public int oldBorder = -1; // -1 mean update screen

        public long oldTime = 0;
        public int oldSpeed = -1; // -1 mean update progressBar
        public int newSpeed = 0;
        public bool showStats = true;

        public void RefreshScreen() {
            Screen.UpdateBorder(Screen.Border);
            for (var addr = 16384; addr < 22528+768; ++addr) {
                Screen.UpdateByte(addr, (byte)mem[addr]);
            }
        }



        public void LoadSnapshot(byte[] bytes) {
            // Crude check but it'll work (SNA is a fixed size)
            if (bytes.Length == 49179) {
                LoadSNA(bytes);
            }
            else {
                LoadZ80(bytes);
            }
        }

        public void LoadROM(byte[] bytes) {
            using var stream = new MemoryStream(bytes);
            ReadBytes(stream, mem, 0, 16384);
        }

        public void LoadSNA(byte[] bytes) {
            using var stream = new MemoryStream(bytes);
            int[] header = new int[27];

            ReadBytes(stream, header, 0, 27);
            ReadBytes(stream, mem, 16384, 49152);

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

            RefreshScreen();
        }

        public void LoadZ80(byte[] bytes) {
            using var stream = new MemoryStream(bytes);
            var bytesLeft = bytes.Length;

            int[] header = new int[30];
            bool compressed = false;

            bytesLeft -= ReadBytes(stream, header, 0, 30);

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
                LoadZ80_extended(stream, bytesLeft);
            }
            else
            if (compressed) {
                /* Old format Z80 snapshot */
                int[] data = new int[bytesLeft];
                int addr = 16384;

                int size = ReadBytes(stream, data, 0, bytesLeft);
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
                ReadBytes(stream, mem, 16384, 49152);
            }

            RefreshScreen();
        }

        private void LoadZ80_extended(Stream stream, int bytesLeft) {
            int[] header = new int[2];
            bytesLeft -= ReadBytes(stream, header, 0, header.Length);

            int type = header[0] | (header[1] << 8);

            switch (type) {
            case 23: /* V2.01 */
                LoadZ80_v201(stream, bytesLeft);
                break;
            case 54: /* V3.00 */
                LoadZ80_v300(stream, bytesLeft);
                break;
            case 58: /* V3.01 */
                LoadZ80_v301(stream, bytesLeft);
                break;
            default:
                throw new Exception("Z80 (extended): unsupported type " + type);
            }
        }

        private void LoadZ80_v201(Stream stream, int bytesLeft) {
            int[] header = new int[23];
            bytesLeft -= ReadBytes(stream, header, 0, header.Length);

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
            ReadBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = LoadZ80_page(data, offset);
            }
        }

        private void LoadZ80_v300(Stream stream, int bytesLeft) {
            int[] header = new int[54];
            bytesLeft -= ReadBytes(stream, header, 0, header.Length);

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
            ReadBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = LoadZ80_page(data, offset);
            }
        }

        private void LoadZ80_v301(Stream stream, int bytesLeft) {
            int[] header = new int[58];
            bytesLeft -= ReadBytes(stream, header, 0, header.Length);

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
            ReadBytes(stream, data, 0, bytesLeft);

            for (int offset = 0, j = 0; j < 3; j++) {
                offset = LoadZ80_page(data, offset);
            }
        }

        private int LoadZ80_page(int[] data, int i) {
            int blocklen;
            int page;

            blocklen = data[i++];
            blocklen |= (data[i++]) << 8;
            page = data[i++];
            var addr = page switch
            {
                4 => 32768,
                5 => 49152,
                8 => 16384,
                _ => throw new Exception("Z80 (page): out of range " + page),
            };
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

        private int ReadBytes(Stream stream, int[] a, int off, int n) {
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
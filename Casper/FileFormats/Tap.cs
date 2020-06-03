using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Casper.FileFormats {
    public class Tap : ISnapshot {
        Queue<byte[]> tape = new Queue<byte[]>();

        public Tap(byte[] bytes) {
            using (var reader = new BinaryReader(new MemoryStream(bytes))) {
                while (reader.PeekChar() != -1) {
                    var blockLength = reader.ReadUInt16();
                    var flag = reader.ReadByte();
                    var data = reader.ReadBytes(blockLength - 2);
                    var checksum = reader.ReadByte();
                    tape.Enqueue(data);
                }
            }
        }

        void ISnapshot.Load(Spectrum spectrum) {
            // Fast new without memory check
            spectrum.PC = (ROM.RAM_DONE);
            spectrum.Steps(3);

            // Override ROM call to LD_BYTES
            spectrum.SetWatchpoint(ROM.LD_BYTES, Handle_LD_BYTES);

            // LOAD "" (ENTER)
            var keys = "j\"\"\r";
            spectrum.Keyboard.OnLogicalKeys(keys);
            spectrum.Steps(keys.Length*Keyboard.LogicKeyInterruptCount*2/*down+up*/);
        }

        void Handle_LD_BYTES(Z80 spectrum) {
            // Destination for data and length are stored in IX and DE resisters
            var dest = spectrum.IX();
            var leng = spectrum.DE();

            // Read next block from virtual tape
            var data = tape.Dequeue();
            spectrum.LoadBytes(dest, data);

            // Execute RET instruction to return to caller with Carry flag true to indicate success
            spectrum.setC(true);
            spectrum.poppc();
        }

        static byte[] SubArray(byte[] array, int offset, int length) {
            var bytes = new byte[length];
            Array.Copy(array, offset, bytes, 0, length);
            return bytes;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casper {
    public class Speaker {
        readonly bool[] samples;

        public event Action<bool[]> PlaySound;

        public Speaker(int numSamples) {
            samples = new bool[numSamples];
        }

        public void Beep(int tstates) {
            samples[tstates] = true;
        }

        public void FlushBuffer() {
            if (samples.Any(s => s)) {
                PlaySound?.Invoke(samples);
                Array.Clear(samples, 0, samples.Length);
            }
        }
    }
}

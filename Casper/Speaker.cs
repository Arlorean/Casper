using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casper {
    public class Speaker {
        double interruptInterval;
        int tstatesPerInterrupt;

        /// <summary>
        /// Called when the speaker is activated.
        /// The time in seconds since the last interrupt is passed as a parameter.
        /// </summary>
        public event Action<double> Activate;

        internal Speaker(int tstatesPerInterrupt) {
            this.interruptInterval = TimeSpan.FromMilliseconds(20).TotalSeconds;
            this.tstatesPerInterrupt = tstatesPerInterrupt;
        }

        internal void Beep(int tstates) {
            var time = ((double)tstates / tstatesPerInterrupt)* interruptInterval;
            Activate?.Invoke(time);
        }
    }
}

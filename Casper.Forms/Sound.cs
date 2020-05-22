using SharpDX.DirectSound;
using SharpDX.Multimedia;
using System;

namespace Casper.Forms {
    public class Sound {
        readonly DirectSound directSound;
        readonly WaveFormat waveFormat;
        readonly SecondarySoundBuffer buffer;
        readonly TimeSpan bufferDuration = TimeSpan.FromMilliseconds(20);
        readonly short[] samples;

        public Sound(IntPtr formHandle) {
            directSound = new DirectSound();
            directSound.SetCooperativeLevel(formHandle, CooperativeLevel.Normal);

            // WAVEFORMATEX Structure (from Microsoft Documentation on DirectSound)
            // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee419019(v%3dvs.85)
            var wFormatTag = WaveFormatEncoding.Pcm;
            var nSamplesPerSec = 44100;
            var nChannels = 1;
            var wBitsPerSample = 16; // (short)
            var nBlockAlign = (nChannels * wBitsPerSample) / 8; // nBlockAlign must be equal to the product of nChannels and wBitsPerSample divided by 8 (bits per byte)
            var nAvgBytesPerSec = nSamplesPerSec*nBlockAlign; // nAvgBytesPerSec should be equal to the product of nSamplesPerSec and nBlockAlign
            waveFormat = WaveFormat.CreateCustomFormat(
                tag: wFormatTag,
                sampleRate: nSamplesPerSec,
                channels: nChannels,
                averageBytesPerSecond: nAvgBytesPerSec,
                blockAlign: nBlockAlign,
                bitsPerSample: wBitsPerSample
            );

            var bufferDesc = new SoundBufferDescription();
            bufferDesc.Format = waveFormat;
            bufferDesc.BufferBytes = Convert.ToInt32(
                bufferDuration.TotalSeconds * waveFormat.AverageBytesPerSecond / waveFormat.Channels);

            buffer = new SecondarySoundBuffer(directSound, bufferDesc);

            int numSamples = buffer.Capabilities.BufferBytes / waveFormat.BlockAlign;
            samples = new short[numSamples];
        }

        public void ActivateSpeaker(double time) {
            var r = time / bufferDuration.TotalSeconds;
            if (r >= 0 && r < 1) {
                var i = (int)(r * samples.Length);
                samples[i] = short.MaxValue;
            }
        }

        public void FlushBuffer() {
            buffer.Write(samples, 0, LockFlags.None);
            buffer.Play(0, PlayFlags.None);
            Array.Clear(samples, 0, samples.Length);
        }
    }
}

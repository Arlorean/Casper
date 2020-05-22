using SharpDX.DirectSound;
using SharpDX.Multimedia;
using System;

namespace Casper.Forms {
    public class Sound {
        readonly DirectSound directSound;
        readonly WaveFormat waveFormat;
        readonly SecondarySoundBuffer buffer;
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

            var bufferDurationSeconds = 0.020;//ms
            var bufferDesc = new SoundBufferDescription();
            bufferDesc.Format = waveFormat;
            bufferDesc.BufferBytes = Convert.ToInt32(
                bufferDurationSeconds * waveFormat.AverageBytesPerSecond / waveFormat.Channels);

            buffer = new SecondarySoundBuffer(directSound, bufferDesc);

            int numSamples = buffer.Capabilities.BufferBytes / waveFormat.BlockAlign;
            samples = new short[numSamples];
        }

        public void PlaySound(bool[] speaker) {
            var r = (float)samples.Length / speaker.Length;
            for (var i=0; i < speaker.Length; ++i) {
                if (speaker[i]) {
                    samples[(int)(i * r)] = short.MaxValue;
                }
            }
            buffer.Write(samples, 0, LockFlags.None);
            buffer.Play(0, PlayFlags.None);
            Array.Clear(samples, 0, samples.Length);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;

namespace ORMonitoring
{
    internal class AudioSplitter : ConsumerProducer<AudioBuffer, AudioBuffer>, IDisposable
    {
        private readonly Queue<DateTime> messageOriginatingTimes;
        private int LorR;
        private DateTime streamStartTime;

        public AudioSplitter(Pipeline pipeline, int LorR,string name = nameof(AudioSplitter))
           : base(pipeline, name)
        {
            this.LorR = LorR;
            this.messageOriginatingTimes = new Queue<DateTime>();
        }

        public void Dispose()
        {
        }

        protected override void Receive(AudioBuffer audioBuffer2, Envelope e)
        {
            var audioBuffer = audioBuffer2.DeepClone();
            this.streamStartTime = e.OriginatingTime;
            this.messageOriginatingTimes.Enqueue(e.OriginatingTime);
            var nbrblock = 4;
            if (audioBuffer.Format.BitsPerSample == 16)
            {
                nbrblock = 2;
            }
            var nbrChannels = audioBuffer.Format.Channels;
            var nbrBits = audioBuffer.Length;
            var bt = new byte[nbrBits / nbrChannels];
            var init = LorR * nbrblock;
            var init2 = 0;

            for (int i = init; i < nbrBits; i = i + nbrChannels * nbrblock)
            {
                for (int j = 0; j < nbrblock; j++)
                {
                    bt[init2] = audioBuffer.Data[i+j];
                    init2 ++;
                }
            }


            var audiobuf2 = new AudioBuffer(bt, WaveFormat.CreatePcm((int) audioBuffer.Format.SamplesPerSec, (int) audioBuffer.Format.BitsPerSample, 1));
            while ((this.messageOriginatingTimes.Count > 0))
            {
                this.Out.Post(audiobuf2, this.messageOriginatingTimes.Dequeue());
            }
        }

    }
}

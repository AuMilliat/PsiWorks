using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.SDL2;
using Microsoft.Psi.Audio;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WebRTC
{

    public class UnityLogger : IDisposable, Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine("[" + eventId + "] " + formatter(state, exception));
        }
    }


    public class WebRTCVideoStreamConfiguration : WebRTConnectorConfiguration
    {
        public string? FFMPEGFullPath { get; set; } = null;// @"C:\Program Files\Tobii\Tobii Pro Lab\Bin";
    }

    public class WebRTCVideoStream : WebRTConnector
    {
        private FFmpegVideoEndPoint VideoDecoder;
        private OpusCodec.OpusAudioEncoder AudioDecoder;
        private WebRTCVideoStreamConfiguration Configuration;

        /// <summary>
        /// Gets the emitter images.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        /// <summary>
        /// Gets the emitter audio.
        /// </summary>
        public Emitter<AudioBuffer> OutAudio { get; private set; }

        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream), DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, configuration, name, defaultDeliveryPolicy)
        {
            Configuration = configuration;
            var logger = new UnityLogger();
            OutImage = parent.CreateEmitter<Shared<Image>>(this, nameof(OutImage));
            OutAudio = parent.CreateEmitter<AudioBuffer>(this, nameof(OutAudio));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, configuration.FFMPEGFullPath, logger);
            VideoDecoder = new FFmpegVideoEndPoint();
            if (configuration.AudioStreaming)
            {
                AudioDecoder = new OpusCodec.OpusAudioEncoder();
            }
        }

        protected override void PrepareActions()
        {
            MediaStreamTrack videoTrack = new MediaStreamTrack(VideoDecoder.GetVideoSourceFormats(), MediaStreamStatusEnum.RecvOnly);
            PeerConnection.addTrack(videoTrack);
            if (Configuration.AudioStreaming)
            {
                MediaStreamTrack audioTrack = new MediaStreamTrack(AudioDecoder.SupportedFormats, MediaStreamStatusEnum.RecvOnly);
                PeerConnection.addTrack(audioTrack);
                PeerConnection.AudioStream.OnRtpPacketReceivedByIndex += AudioStream_OnRtpPacketReceivedByIndex;
            }
            PeerConnection.OnVideoFormatsNegotiated += WebRTCPeer_OnVideoFormatsNegotiated;
            PeerConnection.OnVideoFrameReceived += PeerConnection_OnVideoFrameReceived;
            VideoDecoder.OnVideoSinkDecodedSample += VideoEncoder_OnVideoSinkDecodedSample;
            //VideoDecoder.OnVideoSinkDecodedSampleFaster += VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }

        private void AudioStream_OnRtpPacketReceivedByIndex(int arg1, System.Net.IPEndPoint arg2, SDPMediaTypesEnum arg3, RTPPacket arg4)
        {
            if (arg3 != SDPMediaTypesEnum.audio)
                return;
            short[] buffer = AudioDecoder.DecodeAudio(arg4.GetBytes(), AudioDecoder.SupportedFormats[0]);
            AudioFormat form = AudioDecoder.SupportedFormats[0];
            //WaveFormat wave = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_UNKNOWN, OpusCodec.OpusAudioEncoder.SAMPLE_RATE, OpusCodec.OpusAudioEncoder.MAX_FRAME_SIZE, 2, OpusCodec.OpusAudioEncoder.MAX_FRAME_SIZE * 2, OpusCodec.OpusAudioEncoder.SAMPLE_RATE * OpusCodec.OpusAudioEncoder.MAX_FRAME_SIZE);

            //AudioBuffer audioBuffer = new AudioBuffer(arg4.GetBytes(), wave);
            //try
            //{
            //    OutAudio.Post(new AudioBuffer(arg4.GetBytes(), wave), DateTime.UtcNow);
            //}
            //catch (Exception ex) { }
        }

        private void PeerConnection_OnVideoFrameReceived(System.Net.IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
        {
            VideoDecoder.SetVideoSourceFormat(arg4);
            VideoDecoder.GotVideoFrame(arg1, arg2, arg3, arg4);
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = GetPixelFormat(pixelFormat);
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            OutImage.Post(imageEnc, DateTime.UtcNow);
        }

        private void VideoDecoder_OnVideoSinkDecodedSampleFaster(RawImage rawImage)
        {
            PixelFormat format = GetPixelFormat(rawImage.PixelFormat);
            Image image = new Image(rawImage.Sample, (int)rawImage.Width, (int)rawImage.Height, (int)rawImage.Stride, PixelFormat.BGR_24bpp);
            Shared<Image> imageS = ImagePool.GetOrCreate((int)rawImage.Width, (int)rawImage.Height, format);
            if(Configuration.PixelStreamingConnection)
                imageS.Resource.CopyFrom(image);
            else
                imageS.Resource.CopyFrom(image.Flip(FlipMode.AlongHorizontalAxis));
            OutImage.Post(imageS, DateTime.UtcNow);
            image.Dispose();
        }

        private PixelFormat GetPixelFormat(VideoPixelFormatsEnum pixelFormat)
        {         
            switch (pixelFormat)
            {
                case VideoPixelFormatsEnum.Bgra:
                    return PixelFormat.BGRA_32bpp;
                case VideoPixelFormatsEnum.Bgr:
                    return PixelFormat.BGR_24bpp;
                case VideoPixelFormatsEnum.Rgb:
                    return PixelFormat.RGB_24bpp;
                default:
                case VideoPixelFormatsEnum.NV12:
                case VideoPixelFormatsEnum.I420:
                    throw new Exception("PixelFormat: " + pixelFormat.ToString() + " not supported.");
            } 
        }

        private void WebRTCPeer_OnVideoFormatsNegotiated(List<VideoFormat> obj)
        {
            VideoFormat format = obj.Last();

            VideoDecoder.SetVideoSourceFormat(format);
            VideoDecoder.SetVideoSinkFormat(format);
        }
    }
}

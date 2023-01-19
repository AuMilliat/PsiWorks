using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Net;

namespace WebRTC
{

    public class WebRTCVideoStreamConfiguration : WebRTConnectorConfiguration
    {
        public string? FFMPEGFullPath { get; set; } = null;// @"C:\Program Files\Tobii\Tobii Pro Lab\Bin";
    }

    public class WebRTCVideoStream : WebRTConnector
    {
        private FFmpegVideoEndPoint VideoDecoder;
        private WebRTCVideoStreamConfiguration Configuration;

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream), DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, configuration, name, defaultDeliveryPolicy)
        {
            Configuration = configuration;
            OutImage = parent.CreateEmitter<Shared<Image>>(this, nameof(OutImage));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, configuration.FFMPEGFullPath);
            VideoDecoder = new FFmpegVideoEndPoint();
            VideoDecoder.OnVideoSinkDecodedSampleFaster += VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }

        protected override void PrepareActions()
        {
            var format = VideoDecoder.GetVideoSinkFormats();
            MediaStreamTrack videoTrack = new MediaStreamTrack(VideoDecoder.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly);
            PeerConnection.addTrack(videoTrack);
            PeerConnection.OnVideoFrameReceived += PeerConnection_OnVideoFrameReceived;
            VideoDecoder.OnVideoSinkDecodedSample += VideoEncoder_OnVideoSinkDecodedSample;
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
    }
}

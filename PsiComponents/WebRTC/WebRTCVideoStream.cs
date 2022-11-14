using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Net;
using static DirectShowLib.MediaSubType;

namespace WebRTC
{

    public class WebRTCVideoStreamConfiguration : WebRTConnectorConfiguration
    {
        public string? FFMPEGFullPath { get; set; } = null;// @"C:\Program Files\Tobii\Tobii Pro Lab\Bin";

        public VideoCodecsEnum VideoCodecsEnum { get; set; } = VideoCodecsEnum.H264;
    }

    public class WebRTCVideoStream : WebRTConnector
    {
        private FFmpegVideoEndPoint VideoDecoder;

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream), DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, configuration, name, defaultDeliveryPolicy)
        {
            OutImage = parent.CreateEmitter<Shared<Image>>(this, nameof(OutImage));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, configuration.FFMPEGFullPath);
            VideoDecoder = new FFmpegVideoEndPoint();
            VideoDecoder.RestrictFormats(format => format.Codec == configuration.VideoCodecsEnum);
           
        }

        protected override void PrepareActions()
        {
            MediaStreamTrack videoTrack = new MediaStreamTrack(VideoDecoder.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly);
            PeerConnection.addTrack(videoTrack);
            PeerConnection.OnVideoFrameReceived += VideoDecoder.GotVideoFrame;
            VideoDecoder.OnVideoSinkDecodedSample += VideoEncoder_OnVideoSinkDecodedSample;
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = PixelFormat.BGRA_32bpp;
            switch (pixelFormat)
            {
                case VideoPixelFormatsEnum.Bgra:
                    //already done
                    break;
                case VideoPixelFormatsEnum.Bgr:
                    format = PixelFormat.BGR_24bpp;
                    break;
                case VideoPixelFormatsEnum.Rgb:
                    format = PixelFormat.RGB_24bpp;
                    break;
                case VideoPixelFormatsEnum.NV12:
                case VideoPixelFormatsEnum.I420:
                    Console.WriteLine("PixelFormat: " + pixelFormat.ToString() + " not supported.");
                    return;
            }
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            OutImage.Post(imageEnc, DateTime.Now);
        }
    }
}

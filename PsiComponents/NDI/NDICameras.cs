using System;
using NewTek;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Psi.Components;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Audio;
using VL.IO.NDI;

namespace ORMonitoring
{
    internal class NDICameras : ISourceComponent, IDisposable
    {
        public Emitter<Shared<Image>> Video { get; private set; }
        public Emitter<Shared<Image>> Video2 { get; private set; }
        public Emitter<Shared<Image>> Video3 { get; private set; }

        private static IntPtr _recvInstancePtr;
        private static IntPtr _recvInstancePtr2;
        private static IntPtr _recvInstancePtr3;

        private static string _source = "MEVO-27VFP (Mevo-27VFP)";
        private static string _source2 = "MEVO-27VHT (Mevo-27VHT)";
        private static string _source3 = "MEVO-27FN3 (Mevo-27FN3)";

        private Thread captureThread = null;
        private bool shutdown = false;
        private Shared<Image> image = null;
        private Shared<Image> image2 = null;
        private Shared<Image> image3 = null;

        private DateTime lastTime1;
        private DateTime lastTime2;
        private DateTime lastTime3;




        public NDICameras(Pipeline pipeline)
        {
            
            ConnectNdi();
            this.Video = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Video));
            this.Video2 = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Video2));
            this.Video3 = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Video3));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();
            this.lastTime1 = DateTime.UtcNow;
            this.lastTime2 = DateTime.UtcNow;
            this.lastTime3 = DateTime.UtcNow;
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            shutdown = true;
            DisconnectNdi();
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
#pragma warning disable SYSLIB0006 // Le type ou le membre est obsolète
                captureThread.Abort();
#pragma warning restore SYSLIB0006 // Le type ou le membre est obsolète
            }
            notifyCompleted();
        }

        private void CaptureThreadProc()
        {
            while (!this.shutdown)
            {
                bool Post1 = false;
                bool Post2 = false;
                bool Post3 = false;

                if (_recvInstancePtr != IntPtr.Zero)
                {
                    // The descriptors
                    NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
                    NDIlib.audio_frame_v2_t audioFrame = new NDIlib.audio_frame_v2_t();
                    NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();

                    switch (NDIlib.recv_capture_v2(_recvInstancePtr, ref videoFrame, ref audioFrame, ref metadataFrame, 1000))
                    {
                        // No data
                        case NDIlib.frame_type_e.frame_type_none:
                            // No data received
                            break;

                        // frame settings - check for extended functionality
                        case NDIlib.frame_type_e.frame_type_status_change:
                            break;

                        // Video data
                        case NDIlib.frame_type_e.frame_type_video:
                            if (videoFrame.p_data == IntPtr.Zero)
                            {
                                // alreays free received frames
                                NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                                break;
                            }

                            if (this.image == null)
                            {
                                image = ImagePool.GetOrCreate(videoFrame.xres, videoFrame.yres, Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp);
                                
                            }

                            image.Resource.CopyFrom(videoFrame.p_data);

                            Post1 = true;
                            
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                            break;
                    }
                }
                
                if (_recvInstancePtr2 != IntPtr.Zero)
                {
                    // The descriptors
                    NDIlib.video_frame_v2_t videoFrame2 = new NDIlib.video_frame_v2_t();
                    NDIlib.audio_frame_v2_t audioFrame2 = new NDIlib.audio_frame_v2_t();
                    NDIlib.metadata_frame_t metadataFrame2 = new NDIlib.metadata_frame_t();

                    switch (NDIlib.recv_capture_v2(_recvInstancePtr2, ref videoFrame2, ref audioFrame2, ref metadataFrame2, 1000))
                    {
                        // No data
                        case NDIlib.frame_type_e.frame_type_none:
                            // No data received
                            break;

                        // frame settings - check for extended functionality
                        case NDIlib.frame_type_e.frame_type_status_change:
                            break;

                        // Video data
                        case NDIlib.frame_type_e.frame_type_video:
                            if (videoFrame2.p_data == IntPtr.Zero)
                            {
                                // alreays free received frames
                                NDIlib.recv_free_video_v2(_recvInstancePtr2, ref videoFrame2);
                                break;
                            }

                            if (this.image2 == null)
                            {
                                image2 = ImagePool.GetOrCreate(videoFrame2.xres, videoFrame2.yres, Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp);
                            }
                            /*Console.WriteLine(videoFrame.timestamp);*/
                            image2.Resource.CopyFrom(videoFrame2.p_data);

                            Post2 = true;
                           
                            NDIlib.recv_free_video_v2(_recvInstancePtr2, ref videoFrame2);
                            break;
                    }

                    if (_recvInstancePtr3 != IntPtr.Zero)
                    {
                        // The descriptors
                        NDIlib.video_frame_v2_t videoFrame3 = new NDIlib.video_frame_v2_t();
                        NDIlib.audio_frame_v2_t audioFrame3 = new NDIlib.audio_frame_v2_t();
                        NDIlib.metadata_frame_t metadataFrame3 = new NDIlib.metadata_frame_t();

                        switch (NDIlib.recv_capture_v2(_recvInstancePtr3, ref videoFrame3, ref audioFrame3, ref metadataFrame3, 1000))
                        {
                            // No data
                            case NDIlib.frame_type_e.frame_type_none:
                                // No data received
                                break;

                            // frame settings - check for extended functionality
                            case NDIlib.frame_type_e.frame_type_status_change:
                                break;

                            // Video data
                            case NDIlib.frame_type_e.frame_type_video:
                                if (videoFrame3.p_data == IntPtr.Zero)
                                {
                                    // alreays free received frames
                                    NDIlib.recv_free_video_v2(_recvInstancePtr3, ref videoFrame2);
                                    break;
                                }

                                if (this.image3 == null)
                                {
                                    image3 = ImagePool.GetOrCreate(videoFrame3.xres, videoFrame3.yres, Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp);
                                }
                                image3.Resource.CopyFrom(videoFrame3.p_data);

                                Post3 = true;

                                NDIlib.recv_free_video_v2(_recvInstancePtr3, ref videoFrame3);
                                break;
                        }

                    if (Post1)
                    {try
                    {
                        this.Video.Post(image, DateTime.UtcNow + TimeSpan.FromMilliseconds(50));
                    }
                    catch { }
                    }

                    if (Post2)
                    {try
                    {
                        this.Video2.Post(image2, DateTime.UtcNow + TimeSpan.FromMilliseconds(50));
                    }
                    catch { }
                    }
                    
                    if (Post3)
                    {try
                    {
                        this.Video3.Post(image3, DateTime.UtcNow + TimeSpan.FromMilliseconds(50));
                    }
                    catch { }
                    }
                        
                    }
                }
            }
        }

        private void DisconnectNdi()
        {
            // Destroy the receiver
            NDIlib.recv_destroy(_recvInstancePtr);
            NDIlib.recv_destroy(_recvInstancePtr2);
            NDIlib.recv_destroy(_recvInstancePtr3);

            // set it to a safe value
            _recvInstancePtr = IntPtr.Zero;
        }

        private static void ConnectNdi()
        {
            Console.WriteLine($"Connecting to '{_source}'...");
            // Note (AurélienM) : to test, correction for compilation!
            byte[] bytesSource1 = UTF.StringToUtf8(_source).ToArray();
            byte[] bytesSourceName1 = UTF.StringToUtf8("Channel 1").ToArray();
            byte[] bytesSource2 = UTF.StringToUtf8(_source2).ToArray();
            byte[] bytesSourceName2 = UTF.StringToUtf8("Channel 2").ToArray();
            byte[] bytesSource3 = UTF.StringToUtf8(_source3).ToArray();
            byte[] bytesSourceName3 = UTF.StringToUtf8("Channel 3").ToArray();
            NDIlib.source_t source_t = new NDIlib.source_t
            {
                p_ndi_name = (IntPtr)bytesSource1[0]
            };

            NDIlib.recv_create_v3_t recvDescription = new NDIlib.recv_create_v3_t
            {
                source_to_connect_to = source_t,
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                allow_video_fields = false,
                p_ndi_recv_name = (IntPtr)bytesSourceName1[0] 
            };
            _recvInstancePtr = NDIlib.recv_create_v3(ref recvDescription);

            Console.WriteLine($"Connecting to '{_source2}'...");

            NDIlib.source_t source_t2 = new NDIlib.source_t
            {
                p_ndi_name = (IntPtr)bytesSource2[0]
            };

            NDIlib.recv_create_v3_t recvDescription2 = new NDIlib.recv_create_v3_t
            {
                source_to_connect_to = source_t2,
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                allow_video_fields = false,
                p_ndi_recv_name = (IntPtr)bytesSourceName2[0]
            };
            _recvInstancePtr2 = NDIlib.recv_create_v3(ref recvDescription2);

            Console.WriteLine($"Connecting to '{_source3}'...");

            NDIlib.source_t source_t3 = new NDIlib.source_t
            {
                p_ndi_name = (IntPtr)bytesSource3[0]
            };

            NDIlib.recv_create_v3_t recvDescription3 = new NDIlib.recv_create_v3_t
            {
                source_to_connect_to = source_t3,
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                allow_video_fields = false,
                p_ndi_recv_name = (IntPtr)bytesSourceName3[0]
            };
            _recvInstancePtr3 = NDIlib.recv_create_v3(ref recvDescription3);
        }

        public void Dispose()
        {

        }

    }

}

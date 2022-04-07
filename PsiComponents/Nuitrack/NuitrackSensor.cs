using nuitrack;
using Microsoft.Psi;
using Microsoft.Psi.DeviceManagement;
using DepthImage = Microsoft.Psi.Imaging.DepthImage;
using Image = Microsoft.Psi.Imaging.Image;
using NuitrackDevice = nuitrack.device.NuitrackDevice;

namespace NuitrackComponent
{
    public class NuitrackSensor : Subpipeline
    {

        /// <summary>
        /// Gets the sensor configuration.
        /// </summary>
        public NuitrackCoreConfiguration? Configuration { get; } = null;

        private static List<CameraDeviceInfo>? allDevices = null;

        /* Begin in/out puts*/

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<Skeleton>> Bodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked hands.
        /// </summary>
        public Emitter<List<UserHands>> Hands { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        public Emitter<List<User>> Users { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        public Emitter<List<UserGesturesState>> Gestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        public Emitter<double> FrameRate { get; private set; }
        // TODO: Add more if needed and renaming properly the reciever & emitter

        /* End in/out puts */
        // Constructor
        private NuitrackCore Core;
        public NuitrackSensor(Pipeline pipeline, NuitrackCoreConfiguration? config = null, DeliveryPolicy? defaultDeliveryPolicy = null, DeliveryPolicy? bodyTrackerDeliveryPolicy = null)
         : base(pipeline, nameof(NuitrackSensor), defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {

            this.Configuration = config ?? new NuitrackCoreConfiguration();

            Core = new NuitrackCore(this, this.Configuration);

            this.ColorImage = Core.ColorImage.BridgeTo(pipeline, nameof(this.ColorImage)).Out;
            this.DepthImage = Core.DepthImage.BridgeTo(pipeline, nameof(this.DepthImage)).Out;
            this.Bodies = Core.Bodies.BridgeTo(pipeline, nameof(this.Bodies)).Out;
            this.Hands = Core.Hands.BridgeTo(pipeline, nameof(this.Hands)).Out;
            this.Users = Core.Users.BridgeTo(pipeline, nameof(this.Users)).Out;
            this.Gestures = Core.Gestures.BridgeTo(pipeline, nameof(this.Gestures)).Out;
            this.FrameRate = Core.FrameRate.BridgeTo(pipeline, nameof(this.FrameRate)).Out;
        }

        public MathNet.Spatial.Euclidean.Point2D getProjCoordinates(MathNet.Spatial.Euclidean.Vector3D position)
        {
            Vector3 point = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            Vector3 proj = Core.toProj(point);
            return new MathNet.Spatial.Euclidean.Point2D(proj.X, proj.Y);
        }

        private static List<CameraDeviceInfo.Sensor.ModeInfo> getVideoModes(List<nuitrack.device.VideoMode> videoModes) 
        {
            List<CameraDeviceInfo.Sensor.ModeInfo> modes = new List<CameraDeviceInfo.Sensor.ModeInfo>();
            foreach (var videoMode in videoModes)
            {
                modes.Add(new CameraDeviceInfo.Sensor.ModeInfo
                {
                    Format = Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp,
                    FrameRateNumerator = (uint)videoMode.fps,
                    FrameRateDenominator = 1,
                    ResolutionWidth = (uint)videoMode.width,
                    ResolutionHeight = (uint)videoMode.height,
                });
            }
            return modes;
        }

        /// <summary>
        /// Gets a list of all available capture devices.
        /// </summary>
        public static IEnumerable<CameraDeviceInfo> AllDevices
        {
            get
            {
                if (allDevices == null)
                {
                    Nuitrack.Init("");
                    allDevices = new List<CameraDeviceInfo>();
                    List<NuitrackDevice> listing = Nuitrack.GetDeviceList();
                    int numDevices = 0;
                    foreach (NuitrackDevice nuiDi in listing)
                    {
                        CameraDeviceInfo di = new CameraDeviceInfo();
                        di.SerialNumber = nuiDi.GetInfo(nuitrack.device.DeviceInfoType.SERIAL_NUMBER);
                        di.FriendlyName = nuiDi.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME) + " - " + di.SerialNumber;
                        di.Sensors = new List<CameraDeviceInfo.Sensor>();
                        di.DeviceId = numDevices++;
                        CameraDeviceInfo.Sensor sensor = new CameraDeviceInfo.Sensor();
                        sensor.Modes = new List<CameraDeviceInfo.Sensor.ModeInfo>();

                        for (int k = 0; k < (int)nuitrack.device.StreamType.Count; k++)
                        {
                            var videoModes = getVideoModes(nuiDi.GetAvailableVideoModes((nuitrack.device.StreamType)k));
                            foreach (var videoMode in videoModes)
                                sensor.Modes.Add(videoMode);
                        }
                        allDevices.Add(di);
                    }
                    Nuitrack.Release();
                }
                return allDevices;
            }
        }
    }
}

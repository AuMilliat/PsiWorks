using Microsoft.Psi;
using Microsoft.Psi.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.Research;
using static Microsoft.Psi.DeviceManagement.CameraDeviceInfo;

namespace Tobii
{
    public class TobbiSensor
    {

        private static List<CameraDeviceInfo>? allDevices = null;

        /* Begin in/out puts*/

        /// <summary>
        /// Emitter of the current gaze data.
        /// </summary>
        public Emitter<TobbiGazeData> Gaze { get; private set; }

        /// <summary>
        /// Emitter of the current HMD gaze data.
        /// </summary>
        public Emitter<TobiiHMDGazeData> HMDGaze { get; private set; }

        /// <summary>
        /// Emitter of the current eye openness data.
        /// </summary>
        public Emitter<TobiiEyeOpennessData> EyeOpenness { get; private set; }

        /// <summary>
        /// Emitter of the current user position guide data.
        /// </summary>
        public Emitter<TobiiUserPositionGuide> UserPositionGuide { get; private set; }

        /// <summary>
        /// Emitter of the current synchronization times data.
        /// </summary>
        public Emitter<TobiiTimeSynchronizationReference> TimeSynchronizationReference { get; private set; }

        /// <summary>
        /// Emitter of the current external signal data.
        /// </summary>
        public Emitter<TobiiExternalSignal> ExternalSignal { get; private set; }

        /// <summary>
        /// Emitter of the current error data.
        /// </summary>
        public Emitter<TobiiError> Error { get; private set; }

        /// <summary>
        /// Emitter of the current warning data.
        /// </summary>
        public Emitter<string> Warnings { get; private set; }

        /// <summary>
        /// Emitter of the current device fault data.
        /// </summary>
        public Emitter<string> DeviceFaults { get; private set; }

        /// <summary>
        /// Emitter of the current eye image data.
        /// </summary>
        public Emitter<TobiiEyeImage> EyeImage { get; private set; }

        /// <summary>
        /// Emitter of the current eye raw image data.
        /// </summary>
        public Emitter<TobiiEyeImageRaw> EyeImageRaw { get; private set; }

        /// <summary>
        /// Emitter of the current gaze frequency data.
        /// </summary>
        public Emitter<float> GazeOutputFrequency { get; private set; }

        /// <summary>
        /// Emitter of the current display area data.
        /// </summary>
        public Emitter<DisplayArea> DisplayArea { get; private set; }

        private TobiiCore Core;

        /// <summary>
        /// Initializes a new instance of the <see cref="TobbiCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        public TobbiSensor(Pipeline pipeline, TobiiCoreConfiguration? config = null)
        {
            Core = new TobiiCore(pipeline, config);

            Gaze = Core.Gaze.BridgeTo(pipeline, nameof(Gaze)).Out;
            HMDGaze = Core.HMDGaze.BridgeTo(pipeline, nameof(HMDGaze)).Out;
            EyeOpenness = Core.EyeOpenness.BridgeTo(pipeline, nameof(EyeOpenness)).Out;
            UserPositionGuide = Core.UserPositionGuide.BridgeTo(pipeline, nameof(UserPositionGuide)).Out;
            TimeSynchronizationReference = Core.TimeSynchronizationReference.BridgeTo(pipeline, nameof(TimeSynchronizationReference)).Out;
            ExternalSignal = Core.ExternalSignal.BridgeTo(pipeline, nameof(ExternalSignal)).Out;
            Error = Core.Error.BridgeTo(pipeline, nameof(Error)).Out;
            Warnings = Core.Warnings.BridgeTo(pipeline, nameof(Warnings)).Out;
            DeviceFaults = Core.DeviceFaults.BridgeTo(pipeline, nameof(DeviceFaults)).Out;
            EyeImage = Core.EyeImage.BridgeTo(pipeline, nameof(EyeImage)).Out;
            EyeImageRaw = Core.EyeImageRaw.BridgeTo(pipeline, nameof(EyeImageRaw)).Out;
            GazeOutputFrequency = Core.GazeOutputFrequency.BridgeTo(pipeline, nameof(GazeOutputFrequency)).Out;
            DisplayArea = Core.DisplayArea.BridgeTo(pipeline, nameof(DisplayArea)).Out;
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
                    EyeTrackerCollection listing = EyeTrackingOperations.FindAllEyeTrackers();
                    allDevices = new List<CameraDeviceInfo>();
                    int numDevices = 0;
                    foreach (IEyeTracker tobbiDi in listing)
                    {
                        CameraDeviceInfo di = new CameraDeviceInfo();
                        di.SerialNumber = tobbiDi.SerialNumber;
                        di.FriendlyName = tobbiDi.DeviceName + " - " + di.SerialNumber;
                        di.DeviceType = "Tobbi - " + tobbiDi.DeviceCapabilities;
                        di.Sensors = null;
                        di.DeviceId = numDevices++;
                        allDevices.Add(di);
                    }
                }
                return allDevices;
            }
        }
    }
}

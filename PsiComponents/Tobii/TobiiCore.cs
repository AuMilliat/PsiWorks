using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Tobii.Research;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Tobii
{
    internal sealed class TobiiCore : ISourceComponent, IDisposable
    {

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

        /// <summary>
        ///  Configuration structure
        /// </summary>
        private readonly TobiiCoreConfiguration Configuration;

        /// <summary>
        ///  Device handle
        /// </summary>
        private IEyeTracker? Device = null;

        /// <summary>
        ///  Timestamp for not sending multiple message et the same time (16 ms precision of DateTime.Now)
        /// </summary>
        private DateTime GazeTimestamp;
        private DateTime HMDGazeTimestamp;
        private DateTime EyeOpennessTimestamp;
        private DateTime UserPositionGuideTimestamp;
        private DateTime TimeSynchronizationReferenceTimestamp;
        private DateTime ExternalSignalTimestamp;
        private DateTime ErrorTimestamp;
        private DateTime WarningsTimestamp;
        private DateTime DeviceFaultsTimestamp;
        private DateTime EyeImageTimestamp;
        private DateTime EyeImageRawTimestamp;
        private DateTime GazeOutputFrequencyTimestamp;
        private DateTime DisplayAreaTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="TobiiCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        public TobiiCore(Pipeline pipeline, TobiiCoreConfiguration? config = null)
        {
            Configuration = config ?? new TobiiCoreConfiguration();

            Gaze = pipeline.CreateEmitter<TobbiGazeData>(this, nameof(Gaze));
            HMDGaze = pipeline.CreateEmitter<TobiiHMDGazeData>(this, nameof(HMDGaze));
            EyeOpenness = pipeline.CreateEmitter<TobiiEyeOpennessData>(this, nameof(EyeOpenness));
            UserPositionGuide = pipeline.CreateEmitter<TobiiUserPositionGuide>(this, nameof(UserPositionGuide));
            TimeSynchronizationReference = pipeline.CreateEmitter<TobiiTimeSynchronizationReference>(this, nameof(TimeSynchronizationReference));
            ExternalSignal = pipeline.CreateEmitter<TobiiExternalSignal>(this, nameof(ExternalSignal));
            Error = pipeline.CreateEmitter<TobiiError>(this, nameof(Error));
            Warnings = pipeline.CreateEmitter<string>(this, nameof(Warnings));
            DeviceFaults = pipeline.CreateEmitter<string>(this, nameof(DeviceFaults));
            EyeImage = pipeline.CreateEmitter<TobiiEyeImage>(this, nameof(EyeImage));
            EyeImageRaw = pipeline.CreateEmitter<TobiiEyeImageRaw>(this, nameof(EyeImageRaw));
            GazeOutputFrequency = pipeline.CreateEmitter<float>(this, nameof(GazeOutputFrequency));
            DisplayArea = pipeline.CreateEmitter<DisplayArea>(this, nameof(DisplayArea));

            GazeTimestamp = DateTime.Now;
            HMDGazeTimestamp = DateTime.Now;
            EyeOpennessTimestamp = DateTime.Now;
            UserPositionGuideTimestamp = DateTime.Now;
            TimeSynchronizationReferenceTimestamp = DateTime.Now;
            ExternalSignalTimestamp = DateTime.Now;
            ErrorTimestamp = DateTime.Now;
            WarningsTimestamp = DateTime.Now;
            DeviceFaultsTimestamp = DateTime.Now;
            EyeImageTimestamp = DateTime.Now;
            EyeImageRawTimestamp = DateTime.Now;
            GazeOutputFrequencyTimestamp = DateTime.Now;
            DisplayAreaTimestamp = DateTime.Now;
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            EyeTrackerCollection devices = EyeTrackingOperations.FindAllEyeTrackers();

            if (devices.Count < Configuration.DeviceIndex)
                throw new ArgumentException("Failed to retrieve device!");
            if (Device != null)
                throw new ArgumentException("Already started!");
            Device = devices.ElementAt(Configuration.DeviceIndex);

            FailedLicenseCollection failedLicences;
            Device.TryApplyLicenses(Configuration.Licenses, out failedLicences);
            if(failedLicences.Count > 0)
                throw new ArgumentException("Failed to apply licences!");

            Device.ApplyCalibrationData(Configuration.Calibration);
            Device.GazeDataReceived += OnGazeData;
            Device.EyeOpennessDataReceived += OnEyeOpennessData;
            Device.UserPositionGuideReceived += OnUserPositionGuide;
            Device.HMDGazeDataReceived += OnHMDGazeData;
            Device.TimeSynchronizationReferenceReceived += OnTimeSynchronizationReference;
            Device.ExternalSignalReceived += OnExternalSignalValue;
            Device.EventErrorOccurred += OnEventError;
            Device.EyeImageReceived += OnEyeImage;
            Device.EyeImageRawReceived += OnEyeImageRaw;
            Device.GazeOutputFrequencyChanged += OnGazeOutputFrequency;
            Device.CalibrationModeEntered += OnCalibrationModeEntered;
            Device.CalibrationModeLeft += OnCalibrationModeLeft;
            Device.CalibrationChanged += OnCalibrationChanged;
            Device.DisplayAreaChanged += OnDisplayArea;
            Device.ConnectionLost += OnConnectionLost;
            Device.ConnectionRestored += OnConnectionRestored;
            Device.TrackBoxChanged += OnTrackBox;
            Device.EyeTrackingModeChanged += OnEyeTrackingModeChanged;
            Device.DeviceFaults += OnDeviceFaults;
            Device.DeviceWarnings += OnDeviceWarnings;

        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
            if (Device == null)
                return;
            Device.GazeDataReceived -= OnGazeData;
            Device.EyeOpennessDataReceived -= OnEyeOpennessData;
            Device.UserPositionGuideReceived -= OnUserPositionGuide;
            Device.HMDGazeDataReceived -= OnHMDGazeData;
            Device.TimeSynchronizationReferenceReceived -= OnTimeSynchronizationReference;
            Device.ExternalSignalReceived -= OnExternalSignalValue;
            Device.EventErrorOccurred -= OnEventError;
            Device.EyeImageReceived -= OnEyeImage;
            Device.EyeImageRawReceived -= OnEyeImageRaw;
            Device.GazeOutputFrequencyChanged -= OnGazeOutputFrequency;
            Device.CalibrationModeEntered -= OnCalibrationModeEntered;
            Device.CalibrationModeLeft -= OnCalibrationModeLeft;
            Device.CalibrationChanged -= OnCalibrationChanged;
            Device.DisplayAreaChanged -= OnDisplayArea;
            Device.ConnectionLost -= OnConnectionLost;
            Device.ConnectionRestored -= OnConnectionRestored;
            Device.TrackBoxChanged -= OnTrackBox;
            Device.EyeTrackingModeChanged -= OnEyeTrackingModeChanged;
            Device.DeviceFaults -= OnDeviceFaults;
            Device.DeviceWarnings -= OnDeviceWarnings;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Device != null)
            {
                Device.Dispose();
                Device = null;
            }
        }

        private void OnGazeData(object sender, GazeDataEventArgs data)
        {
            if (Device == sender && GazeTimestamp != DateTime.Now)
            {
                GazeTimestamp = DateTime.Now;
                Gaze.Post(new TobbiGazeData(data), GazeTimestamp);
            }
        }
        
        private void OnHMDGazeData(object sender, HMDGazeDataEventArgs data)
        {
            if (Device == sender && HMDGazeTimestamp != DateTime.Now)
            {
                HMDGazeTimestamp = DateTime.Now;
                HMDGaze.Post(new TobiiHMDGazeData(data), HMDGazeTimestamp);
            }
        }

        private void OnEyeOpennessData(object sender, EyeOpennessDataEventArgs data)
        {
            if (Device == sender && EyeOpennessTimestamp != DateTime.Now)
            {
                EyeOpennessTimestamp = DateTime.Now;
                EyeOpenness.Post(new TobiiEyeOpennessData(data), EyeOpennessTimestamp);
            }
        }

        private void OnUserPositionGuide(object sender, UserPositionGuideEventArgs data)
        {
            if (Device == sender && UserPositionGuideTimestamp != DateTime.Now)
            {
                UserPositionGuideTimestamp = DateTime.Now;
                UserPositionGuide.Post(new TobiiUserPositionGuide(data), UserPositionGuideTimestamp);
            }
        }

        private void OnTimeSynchronizationReference(object sender, TimeSynchronizationReferenceEventArgs data)
        {
            if (Device == sender && TimeSynchronizationReferenceTimestamp != DateTime.Now)
            {
                TimeSynchronizationReferenceTimestamp = DateTime.Now;
                TimeSynchronizationReference.Post(new TobiiTimeSynchronizationReference(data), TimeSynchronizationReferenceTimestamp);
            }
        }

        private void OnExternalSignalValue(object sender, ExternalSignalValueEventArgs data)
        {
            if (Device == sender && ExternalSignalTimestamp != DateTime.Now)
            {
                ExternalSignalTimestamp = DateTime.Now;
                ExternalSignal.Post(new TobiiExternalSignal(data), ExternalSignalTimestamp);
            }
        }

        private void OnEventError(object sender, EventErrorEventArgs data)
        {
            if (Device == sender && ErrorTimestamp != DateTime.Now)
            {
                ErrorTimestamp = DateTime.Now;
                Error.Post(new TobiiError(data), ErrorTimestamp);
            }
        }

        private void OnEyeImage(object sender, EyeImageEventArgs data)
        {
            if (Device == sender && EyeImageTimestamp != DateTime.Now)
            {
                EyeImageTimestamp = DateTime.Now;
                EyeImage.Post(new TobiiEyeImage(data), EyeImageTimestamp);
            }
        }

        private void OnEyeImageRaw(object sender, EyeImageRawEventArgs data)
        {
            if (Device == sender && EyeImageRawTimestamp != DateTime.Now)
            {
                EyeImageRawTimestamp = DateTime.Now;
                EyeImageRaw.Post(new TobiiEyeImageRaw(data), EyeImageRawTimestamp);
            }
        }

        private void OnGazeOutputFrequency(object sender, GazeOutputFrequencyEventArgs data)
        {
            if (Device == sender && GazeOutputFrequencyTimestamp != DateTime.Now)
            {
                GazeOutputFrequencyTimestamp = DateTime.Now;
                GazeOutputFrequency.Post(data.GazeOutputFrequency, GazeOutputFrequencyTimestamp);
            }
        }

        private void OnCalibrationModeEntered(object sender, CalibrationModeEnteredEventArgs data) { /*timestamp*/}
        private void OnCalibrationModeLeft(object sender, CalibrationModeLeftEventArgs data) {/*timestamp*/ }
        private void OnCalibrationChanged(object sender, CalibrationChangedEventArgs data) {/*timestamp*/ }

        private void OnDisplayArea(object sender, DisplayAreaEventArgs data)
        {
            if (Device == sender && DisplayAreaTimestamp != DateTime.Now)
            {
                DisplayAreaTimestamp = DateTime.Now;
                DisplayArea.Post(data.DisplayArea, DisplayAreaTimestamp);
            }
        }

        private void OnConnectionLost(object sender, ConnectionLostEventArgs data) {/*timestamp*/ }
        private void OnConnectionRestored(object sender, ConnectionRestoredEventArgs data) { /*timestamp*/ }
        private void OnTrackBox(object sender, TrackBoxEventArgs data) { /*timestamp*/ }
        private void OnEyeTrackingModeChanged(object sender, EyeTrackingModeChangedEventArgs data) { /*timestamp*/ }

        private void OnDeviceFaults(object sender, DeviceFaultsEventArgs data)
        {
            if (Device == sender && DeviceFaultsTimestamp != DateTime.Now)
            {
                DeviceFaultsTimestamp = DateTime.Now;
                DeviceFaults.Post(data.Faults, DeviceFaultsTimestamp);
            }
        }

        private void OnDeviceWarnings(object sender, DeviceWarningsEventArgs data)
        {
            if (Device == sender && WarningsTimestamp != DateTime.Now)
            {
                WarningsTimestamp = DateTime.Now;  
                Warnings.Post(data.Warnings, WarningsTimestamp);
            }
        }
    }
}

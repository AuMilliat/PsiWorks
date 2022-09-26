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
        private readonly TobiiCoreConfiguration Configuration;

        private IEyeTracker? Device = null;

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
        }

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
        public Emitter<TobiiTimeSynchronizationReference> TimeSynchronizationReference{ get; private set; }

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
            if (Device == sender)
                Gaze.Post(new TobbiGazeData(data), DateTime.Now);
        }
        
        private void OnHMDGazeData(object sender, HMDGazeDataEventArgs data)
        {
            if (Device == sender)
                HMDGaze.Post(new TobiiHMDGazeData(data), DateTime.Now);
        }

        private void OnEyeOpennessData(object sender, EyeOpennessDataEventArgs data)
        {
            if (Device == sender)
                EyeOpenness.Post(new TobiiEyeOpennessData(data), DateTime.Now);
        }

        private void OnUserPositionGuide(object sender, UserPositionGuideEventArgs data)
        {
            if (Device == sender)
                UserPositionGuide.Post(new TobiiUserPositionGuide(data), DateTime.Now);
        }

        private void OnTimeSynchronizationReference(object sender, TimeSynchronizationReferenceEventArgs data)
        {
            if (Device == sender)
                TimeSynchronizationReference.Post(new TobiiTimeSynchronizationReference(data), DateTime.Now);
        }

        private void OnExternalSignalValue(object sender, ExternalSignalValueEventArgs data)
        {
            if (Device == sender)
                ExternalSignal.Post(new TobiiExternalSignal(data), DateTime.Now);
        }

        private void OnEventError(object sender, EventErrorEventArgs data)
        {
            if (Device == sender)
                Error.Post(new TobiiError(data), DateTime.Now);
        }

        private void OnEyeImage(object sender, EyeImageEventArgs data)
        {
            if (Device == sender)
                EyeImage.Post(new TobiiEyeImage(data), DateTime.Now);
        }

        private void OnEyeImageRaw(object sender, EyeImageRawEventArgs data)
        {
            if (Device == sender)
                EyeImageRaw.Post(new TobiiEyeImageRaw(data), DateTime.Now);
        }

        private void OnGazeOutputFrequency(object sender, GazeOutputFrequencyEventArgs data)
        {
            if (Device == sender)
                GazeOutputFrequency.Post(data.GazeOutputFrequency, DateTime.Now);
        }

        private void OnCalibrationModeEntered(object sender, CalibrationModeEnteredEventArgs data) { /*timestamp*/}
        private void OnCalibrationModeLeft(object sender, CalibrationModeLeftEventArgs data) {/*timestamp*/ }
        private void OnCalibrationChanged(object sender, CalibrationChangedEventArgs data) {/*timestamp*/ }

        private void OnDisplayArea(object sender, DisplayAreaEventArgs data)
        {
            if (Device == sender)
                DisplayArea.Post(data.DisplayArea, DateTime.Now);
        }

        private void OnConnectionLost(object sender, ConnectionLostEventArgs data) {/*timestamp*/ }
        private void OnConnectionRestored(object sender, ConnectionRestoredEventArgs data) { /*timestamp*/ }
        private void OnTrackBox(object sender, TrackBoxEventArgs data) { /*timestamp*/ }
        private void OnEyeTrackingModeChanged(object sender, EyeTrackingModeChangedEventArgs data) { /*timestamp*/ }

        private void OnDeviceFaults(object sender, DeviceFaultsEventArgs data)
        {
            if (Device == sender)
                DeviceFaults.Post(data.Faults, DateTime.Now);
        }

        private void OnDeviceWarnings(object sender, DeviceWarningsEventArgs data)
        {
            if (Device == sender)
                Warnings.Post(data.Warnings, DateTime.Now);
        }
    }
}

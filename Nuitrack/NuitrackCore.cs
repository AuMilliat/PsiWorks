using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using nuitrack;
using nuitrack.device;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using DepthImage = Microsoft.Psi.Imaging.DepthImage;
using Image = Microsoft.Psi.Imaging.Image;

namespace NuitrackComponent
{
    internal sealed class NuitrackCore : ISourceComponent, IDisposable
    {
        private Thread? captureThread = null;
        private readonly NuitrackCoreConfiguration configuration;

        private ColorSensor? colorSensor = null;
        private DepthSensor? depthSensor = null;
        private SkeletonTracker? skeletonTracker = null;
        private HandTracker? handTracker = null;
        private UserTracker? userTracker = null;
        private GestureRecognizer? gestureRecognizer = null;
        private object? waitingObject = null;

        private bool shutdown = false;

        /// <summary>
        /// The underlying Nuitrack device.
        /// </summary>
        /// 
        private static readonly object CameraOpenLock = new object();
        private NuitrackDevice? device = null;

        private long colorTimestamp = 0;
        private long depthTimestamp = 0;
        private long skeletonTimestamp = 0;
        private long handTimestamp = 0;
        private long userTimestamp = 0;
        private long gestureTimestamp = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="NuitrackCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        public NuitrackCore(Pipeline pipeline, NuitrackCoreConfiguration? config = null)
        {
            this.configuration = config ?? new NuitrackCoreConfiguration();

           
            this.DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(this.DepthImage));
            this.ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.ColorImage));
            this.Bodies = pipeline.CreateEmitter<List<Skeleton>>(this, nameof(this.Bodies));
            this.Hands = pipeline.CreateEmitter<List<UserHands>>(this, nameof(this.Hands));
            this.Users = pipeline.CreateEmitter<List<User>>(this, nameof(this.Users));
            this.Gestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, nameof(this.Gestures));
            this.FrameRate = pipeline.CreateEmitter<double>(this, nameof(this.FrameRate));

        }

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

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public static int GetInstalledCount()
        {
            return 0;// Device.GetInstalledCount();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.device != null)
            {
                Nuitrack.Release();
                this.device = null;
            }
        }
        private void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            if (depthFrame != null && depthTimestamp != (long)depthFrame.Timestamp)
            {
                Shared<DepthImage> image = Microsoft.Psi.Imaging.DepthImagePool.GetOrCreate(depthFrame.Cols, depthFrame.Rows);
                depthTimestamp = (long)depthFrame.Timestamp;
                image.Resource.CopyFrom(depthFrame.Data);
                this.DepthImage.Post(image, DateTime.FromFileTime(depthTimestamp));
                depthFrame.Dispose();
            }
        }

        private void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (colorFrame != null && colorTimestamp != (long)colorFrame.Timestamp)
            {
                Shared<Image> image = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(colorFrame.Cols, colorFrame.Rows, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                colorTimestamp = (long)colorFrame.Timestamp;
                image.Resource.CopyFrom(colorFrame.Data);
                this.ColorImage.Post(image, DateTime.FromFileTime(colorTimestamp));
                colorFrame.Dispose();
            }
        }

        private void onSkeletonUpdate(SkeletonData skeletonData)
        {
            if (skeletonData != null && skeletonData.NumUsers > 0 && skeletonTimestamp != (long)skeletonData.Timestamp)
            {
                List<Skeleton> output = new List<Skeleton>();
                foreach(Skeleton body in skeletonData.Skeletons)
                    output.Add(body);
                skeletonTimestamp = (long)skeletonData.Timestamp;
                this.Bodies.Post(output, DateTime.FromFileTime(skeletonTimestamp));
                skeletonData.Dispose();
            }
        }

        private void onHandUpdate(HandTrackerData handData)
        {
            if (handData != null && handData.NumUsers > 1 && handTimestamp != (long)handData.Timestamp)
            {
                List<UserHands> output = new List<UserHands>();
                foreach (UserHands hand in handData.UsersHands)
                    output.Add(hand);
                handTimestamp = (long)handData.Timestamp;
                this.Hands.Post(output, DateTime.FromFileTime(handTimestamp));
                handData.Dispose();
            }
        }

        private void onUserUpdate(UserFrame userFrame)
        {
            if (userFrame != null && userFrame.NumUsers > 0 && userTimestamp != (long)userFrame.Timestamp)
            {
                List<User> output = new List<User>();
                foreach (User user in userFrame.Users)
                    output.Add(user);
                userTimestamp = (long)userFrame.Timestamp;
                this.Users.Post(output, DateTime.FromFileTime(userTimestamp));
                userFrame.Dispose();
            }
        }

        private void onGestureUpdate(UserGesturesStateData gestureData)
        {
            if (gestureData != null && gestureData.NumUsersGesturesStates > 0 && gestureTimestamp != (long)gestureData.Timestamp)
            {
                List<UserGesturesState> output = new List<UserGesturesState>();
                foreach (UserGesturesState gesture in gestureData.UserGesturesStates)
                    output.Add(gesture);
                gestureTimestamp = (long)gestureData.Timestamp;
                this.Gestures.Post(output, DateTime.FromFileTime(gestureTimestamp));
                gestureData.Dispose();
            }
        }


        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Prevent device open race condition.
            lock (CameraOpenLock)
            {
                Nuitrack.Init("");
                List<NuitrackDevice> devices = Nuitrack.GetDeviceList();
                this.device = devices[this.configuration.DeviceIndex];
                Nuitrack.SetDevice(this.device);
            }
            try
            {
                // activate selected device
                bool isActivated = Convert.ToBoolean(device.GetActivationStatus());
                if (!isActivated)
                {
                    device.Activate(this.configuration.ActivationKey);
                    if(!Convert.ToBoolean(device.GetActivationStatus()))
                        throw new ArgumentException("Invalid activation key!");
                }

                if(this.configuration.OutputColor)
                {
                    colorSensor = ColorSensor.Create();
                    colorSensor.OnUpdateEvent += onColorSensorUpdate;
                    waitingObject = colorSensor;
                }

                if (this.configuration.OutputDepth)
                {
                    depthSensor = DepthSensor.Create();
                    depthSensor.OnUpdateEvent += onDepthSensorUpdate;
                    waitingObject = depthSensor;
                }

                if (this.configuration.OutputSkeletonTracking)
                {
                    skeletonTracker = SkeletonTracker.Create();
                    skeletonTracker.SetAutoTracking(true);
                    skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;
                    waitingObject = skeletonTracker;
                }

                if (this.configuration.OutputHandTracking)
                {
                    handTracker = HandTracker.Create();
                    handTracker.OnUpdateEvent += onHandUpdate;
                }

                if (this.configuration.OutputUserTracking)
                {
                    userTracker = UserTracker.Create();
                    userTracker.OnUpdateEvent += onUserUpdate;
                }

                if (this.configuration.OutputGestureRecognizer)
                {
                    gestureRecognizer = GestureRecognizer.Create();
                    gestureRecognizer.OnUpdateEvent += onGestureUpdate;
                }

                Nuitrack.Run();
                this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
                this.captureThread.Start();
            }
            catch (nuitrack.Exception exception)
            {
                throw new ArgumentException("Invalid operation: " + exception.ToString());
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            shutdown = true;
            Nuitrack.Release();
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                this.captureThread.Abort();
            }
            notifyCompleted();
        }

        private void CaptureThreadProc()
        {
            nuitrack.Module? waitingObject = null;
            if (skeletonTracker != null)
                waitingObject = skeletonTracker;
            else if (handTracker != null)
                waitingObject = handTracker;
            if (userTracker != null)
                waitingObject = userTracker;
            if (gestureRecognizer != null)
                waitingObject = gestureRecognizer;
            else if(colorSensor != null)
                waitingObject= colorSensor;
            else if (depthSensor != null)
                waitingObject = depthSensor;
            if(waitingObject == null)
                throw new ArgumentException("No tracker available");
            while (this.device != null && !this.shutdown)
            { 
                Nuitrack.WaitUpdate(waitingObject);
            }
        }

    }
}

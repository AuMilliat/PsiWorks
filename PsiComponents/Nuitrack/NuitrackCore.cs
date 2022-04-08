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
        private Thread? CaptureThread = null;
        private readonly NuitrackCoreConfiguration Configuration;

        private ColorSensor? ColorSensor = null;
        private DepthSensor? DepthSensor = null;
        private SkeletonTracker? SkeletonTracker = null;
        private HandTracker? HandTracker = null;
        private UserTracker? UserTracker = null;
        private GestureRecognizer? GestureRecognizer = null;
        private object? WaitingObject = null;

        private bool Shutdown = false;

        /// <summary>
        /// The underlying Nuitrack device.
        /// </summary>
        /// 
        private static readonly object CameraOpenLock = new object();
        private NuitrackDevice? Device = null;

        private long ColorTimestamp = 0;
        private long DepthTimestamp = 0;
        private long SkeletonTimestamp = 0;
        private long HandTimestamp = 0;
        private long UserTimestamp = 0;
        private long GestureTimestamp = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="NuitrackCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        public NuitrackCore(Pipeline pipeline, NuitrackCoreConfiguration? config = null)
        {
            Configuration = config ?? new NuitrackCoreConfiguration();

           
            DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(DepthImage));
            ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(ColorImage));
            Bodies = pipeline.CreateEmitter<List<Skeleton>>(this, nameof(Bodies));
            Hands = pipeline.CreateEmitter<List<UserHands>>(this, nameof(Hands));
            Users = pipeline.CreateEmitter<List<User>>(this, nameof(Users));
            Gestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, nameof(Gestures));
            FrameRate = pipeline.CreateEmitter<double>(this, nameof(FrameRate));

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
            if (Device != null)
            {
                Nuitrack.Release();
                Device = null;
            }
        }

        public Vector3 toProj(Vector3 point)
        {
            if(DepthSensor != null)
                return DepthSensor.ConvertRealToProjCoords(point);
            return point;
        }
        private void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            if (depthFrame != null && DepthTimestamp != (long)depthFrame.Timestamp)
            {
                Shared<DepthImage> image = Microsoft.Psi.Imaging.DepthImagePool.GetOrCreate(depthFrame.Cols, depthFrame.Rows);
                DepthTimestamp = (long)depthFrame.Timestamp;
                image.Resource.CopyFrom(depthFrame.Data);
                DepthImage.Post(image, DateTime.UtcNow);
                depthFrame.Dispose();
            }
        }

        private void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (colorFrame != null && ColorTimestamp != (long)colorFrame.Timestamp)
            {
                Shared<Image> image = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(colorFrame.Cols, colorFrame.Rows, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                ColorTimestamp = (long)colorFrame.Timestamp;
                image.Resource.CopyFrom(colorFrame.Data);
                ColorImage.Post(image, DateTime.UtcNow);
                colorFrame.Dispose();
            }
        }

        private void onSkeletonUpdate(SkeletonData skeletonData)
        {
            if (skeletonData != null && skeletonData.NumUsers > 0 && SkeletonTimestamp != (long)skeletonData.Timestamp)
            {
                List<Skeleton> output = new List<Skeleton>();
                foreach(Skeleton body in skeletonData.Skeletons)
                    output.Add(body);
                SkeletonTimestamp = (long)skeletonData.Timestamp;
                Bodies.Post(output, DateTime.UtcNow);
                skeletonData.Dispose();
            }
        }

        private void onHandUpdate(HandTrackerData handData)
        {
            if (handData != null && handData.NumUsers > 1 && HandTimestamp != (long)handData.Timestamp)
            {
                List<UserHands> output = new List<UserHands>();
                foreach (UserHands hand in handData.UsersHands)
                    output.Add(hand);
                HandTimestamp = (long)handData.Timestamp;
                Hands.Post(output, DateTime.UtcNow);
                handData.Dispose();
            }
        }

        private void onUserUpdate(UserFrame userFrame)
        {
            if (userFrame != null && userFrame.NumUsers > 0 && UserTimestamp != (long)userFrame.Timestamp)
            {
                List<User> output = new List<User>();
                foreach (User user in userFrame.Users)
                    output.Add(user);
                UserTimestamp = (long)userFrame.Timestamp;
                Users.Post(output, DateTime.UtcNow);
                userFrame.Dispose();
            }
        }

        private void onGestureUpdate(UserGesturesStateData gestureData)
        {
            if (gestureData != null && gestureData.NumUsersGesturesStates > 0 && GestureTimestamp != (long)gestureData.Timestamp)
            {
                List<UserGesturesState> output = new List<UserGesturesState>();
                foreach (UserGesturesState gesture in gestureData.UserGesturesStates)
                    output.Add(gesture);
                GestureTimestamp = (long)gestureData.Timestamp;
                Gestures.Post(output, DateTime.UtcNow);
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
                Device = devices[Configuration.DeviceIndex];
                Nuitrack.SetDevice(Device);
            }
            try
            {
                // activate selected device
                bool isActivated = Convert.ToBoolean(Device.GetActivationStatus());
                if (!isActivated)
                {
                    Device.Activate(Configuration.ActivationKey);
                    if(!Convert.ToBoolean(Device.GetActivationStatus()))
                        throw new ArgumentException("Invalid activation key!");
                }

                if(Configuration.OutputColor)
                {
                    ColorSensor = ColorSensor.Create();
                    ColorSensor.OnUpdateEvent += onColorSensorUpdate;
                    WaitingObject = ColorSensor;
                }

                if (Configuration.OutputDepth)
                {
                    DepthSensor = DepthSensor.Create();
                    DepthSensor.OnUpdateEvent += onDepthSensorUpdate;
                    WaitingObject = DepthSensor;
                }

                if (Configuration.OutputSkeletonTracking)
                {
                    SkeletonTracker = SkeletonTracker.Create();
                    SkeletonTracker.SetAutoTracking(true);
                    SkeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;
                    WaitingObject = SkeletonTracker;
                }

                if (Configuration.OutputHandTracking)
                {
                    HandTracker = HandTracker.Create();
                    HandTracker.OnUpdateEvent += onHandUpdate;
                }

                if (Configuration.OutputUserTracking)
                {
                    UserTracker = UserTracker.Create();
                    UserTracker.OnUpdateEvent += onUserUpdate;
                }

                if (Configuration.OutputGestureRecognizer)
                {
                    GestureRecognizer = GestureRecognizer.Create();
                    GestureRecognizer.OnUpdateEvent += onGestureUpdate;
                }

                Nuitrack.Run();
                CaptureThread = new Thread(new ThreadStart(CaptureThreadProc));
                CaptureThread.Start();
            }
            catch (nuitrack.Exception exception)
            {
                throw new ArgumentException("Invalid operation: " + exception.ToString());
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Shutdown = true;
            Nuitrack.Release();
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (CaptureThread != null && CaptureThread.Join(waitTime) != true)
                CaptureThread.Abort();
            notifyCompleted();
        }

        private void CaptureThreadProc()
        {
            nuitrack.Module? WaitingObject = null;
            if (SkeletonTracker != null)
                WaitingObject = SkeletonTracker;
            else if (HandTracker != null)
                WaitingObject = HandTracker;
            if (UserTracker != null)
                WaitingObject = UserTracker;
            if (GestureRecognizer != null)
                WaitingObject = GestureRecognizer;
            else if(ColorSensor != null)
                WaitingObject= ColorSensor;
            else if (DepthSensor != null)
                WaitingObject = DepthSensor;
            if(WaitingObject == null)
                throw new ArgumentException("No tracker available");
            while (Device != null && !Shutdown)
                Nuitrack.WaitUpdate(WaitingObject);
        }

    }
}

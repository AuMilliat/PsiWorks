using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Components;
using Groups.Instant;
using Groups.Integrated;
using BodiesDetection;
using CalibrationByBodies;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PsiWork_WPF
{
    internal sealed class KeyboardReader : Microsoft.Psi.Components.ISourceComponent, IProducer<string>
    {
        public Emitter<string> Out { get; private set; }

        public KeyboardReader(Pipeline pipeline)
        {
            Out = pipeline.CreateEmitter<string>(this, nameof(this.Out));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }

        public void Capture(string message)
        {
            Out.Post(message, DateTime.UtcNow);
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IProducer<bool>
    {
        public Emitter<bool> Out { get; private set; }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer Visu0 { get; }
        public AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer Visu1 { get; }
        public AzureKinectBodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer Calib { get; }

        private string status = "";
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        public void DelegateMethod(string status)
        {
            Status = status;
        }

        private Pipeline pipeline;
        public MainWindow()
        {
            DataContext = this;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create(enableDiagnostics: true);
            Out = pipeline.CreateEmitter<bool>(this, nameof(this.Out));

            /*** KINECT SENSOR ***/
            AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
            configKinect0.DeviceIndex = 0;
            configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor0 = new AzureKinectSensor(pipeline, configKinect0);

            AzureKinectSensorConfiguration configKinect1 = new AzureKinectSensorConfiguration();
            configKinect1.DeviceIndex = 1;
            configKinect1.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor1 = new AzureKinectSensor(pipeline, configKinect1);

            /*** BODIES VISUALIZERS ***/
            Visu0 = new AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            Visu1 = new AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            // Linkage
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Visu0.InCalibration);
            sensor0.Bodies.PipeTo(Visu0.InBodies);
            sensor0.ColorImage.PipeTo(Visu0.InColorImage);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(Visu1.InCalibration);
            sensor1.Bodies.PipeTo(Visu1.InBodies);
            sensor1.ColorImage.PipeTo(Visu1.InColorImage);

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "kinectecConverter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "kinectecConverter1");

            /*** CALIBRATION BY BODIES ***/
            CalibrationByBodiesConfiguration calibrationByBodiesConfiguration = new CalibrationByBodiesConfiguration();
            calibrationByBodiesConfiguration.ConfidenceLevelForCalibration = Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            calibrationByBodiesConfiguration.SetStatus = DelegateMethod;
            CalibrationByBodies.CalibrationByBodies calibrationByBodies = new CalibrationByBodies.CalibrationByBodies(pipeline, calibrationByBodiesConfiguration);

            /*** CALIBRATION VISUALIZER ***/
            Calib = new AzureKinectBodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer(pipeline);
            // Linkage
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Calib.InCalibrationMaster);
            calibrationByBodies.OutCalibration.PipeTo(Calib.InCalibrationSlave);
            sensor0.Bodies.PipeTo(Calib.InBodiesMaster);
            sensor0.ColorImage.PipeTo(Calib.InColorImage);
            sensor1.Bodies.PipeTo(Calib.InBodiesSlave);

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesDetectionConfiguration bodiesDetectionConfiguration = new BodiesDetectionConfiguration();
            BodiesDetection.BodiesDetection bodiesDetection = new BodiesDetection.BodiesDetection(pipeline, bodiesDetectionConfiguration);

            /*** POSITION SELECTER ***/
            // Basic configuration for the moment.
            SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
            SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(pipeline, bodiesSelectionConfiguration);

            /*** INSTANT GROUPS ***/
            // Basic configuration for the moment.
            InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
            InstantGroups instantGroups = new InstantGroups(pipeline, instantGroupsConfiguration);

            /*** INTEGRATED GROUPS ***/
            // Basic configuration for the moment.
            IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
            IntegratedGroups intgratedGroups = new IntegratedGroups(pipeline, integratedGroupsConfiguration);

            /*** MORE TO COME ! ***/


            /*** LINKAGE ***/
            Out.PipeTo(calibrationByBodies.InSynchEvent);
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);
            bodiesConverter0.OutBodies.PipeTo(calibrationByBodies.InCamera1Bodies);
            bodiesConverter1.OutBodies.PipeTo(calibrationByBodies.InCamera2Bodies);
            calibrationByBodies.OutCalibration.PipeTo(bodiesDetection.InCalibrationMatrix);
            bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
            bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
            positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);
            instantGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);

            // RunAsync the pipeline in non-blocking mode.
            pipeline.RunAsync();
            InitializeComponent();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Stop correctly the pipeline.
            pipeline.Dispose();
            base.OnClosing(e);
            var window = Window.GetWindow(this);
            window.KeyDown -= HandleKeyPress;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Out.Post(true, DateTime.UtcNow);
        }
    }
}

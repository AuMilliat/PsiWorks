using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Components;
using Groups.Instant;
using Groups.Integrated;
using Bodies;
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
           

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "kinectecConverter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "kinectecConverter1");

            /*** BODIES IDENTIFICATION ***/
            BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
            BodiesIdentification bodiesIdentification0 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);
            BodiesIdentification bodiesIdentification1 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);

            /*** CALIBRATION BY BODIES ***/
            CalibrationByBodiesConfiguration calibrationByBodiesConfiguration = new CalibrationByBodiesConfiguration();
            calibrationByBodiesConfiguration.ConfidenceLevelForCalibration = Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            calibrationByBodiesConfiguration.SetStatus = DelegateMethod;
            CalibrationByBodies.CalibrationByBodies calibrationByBodies = new CalibrationByBodies.CalibrationByBodies(pipeline, calibrationByBodiesConfiguration);

            /*** CALIBRATION VISUALIZER ***/
            Calib = new AzureKinectBodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer(pipeline);
       

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesDetectionConfiguration bodiesDetectionConfiguration = new BodiesDetectionConfiguration();
            BodiesDetection bodiesDetection = new BodiesDetection(pipeline, bodiesDetectionConfiguration);

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
            // Sensor0 -> Converter0 -> Identificator0 -> Visu0      |  
            //                                         -> Calibration -> Detector -> Extractor -> Instant -> Integrated
            // Sensor1 -> Converter1 -> Identificator1 -> Visu0      |-> VisuCalib                       |-> Entry

            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
            //identificator0
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);
            //visu0
            sensor0.ColorImage.PipeTo(Visu0.InColorImage);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Visu0.InCalibration);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(Visu0.InBodies);

            //converter1
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);
            //identificator1
            bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);
            //visu1
            sensor1.ColorImage.PipeTo(Visu1.InColorImage);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(Visu1.InCalibration);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(Visu1.InBodies);

            //calib
            Out.PipeTo(calibrationByBodies.InSynchEvent);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera1Bodies);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera2Bodies);

            //detector
            calibrationByBodies.OutCalibration.PipeTo(bodiesDetection.InCalibrationMatrix);
            bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);

            //visucalib
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Calib.InCalibrationMaster);
            sensor0.ColorImage.PipeTo(Calib.InColorImage);
            calibrationByBodies.OutCalibration.PipeTo(Calib.InCalibrationSlave);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(Calib.InBodiesMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(Calib.InBodiesSlave);

            //extractor
            bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);

            //Instant
            positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);

            //integrated
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

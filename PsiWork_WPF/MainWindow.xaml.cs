using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Groups.Instant;
using Groups.Integrated;
using BodiesDetection;

namespace PsiWork_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer Visu0 { get; }
        public AzureKinectBodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer Visu1 { get; }

        private Pipeline pipeline;
        public MainWindow()
        {
            DataContext = this;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create(enableDiagnostics: true);

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
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Visu0.CalibrationIn);
            sensor0.Bodies.PipeTo(Visu0.BodiesIn);
            sensor0.ColorImage.PipeTo(Visu0.ColorImageIn);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(Visu1.CalibrationIn);
            sensor1.Bodies.PipeTo(Visu1.BodiesIn);
            sensor1.ColorImage.PipeTo(Visu1.ColorImageIn);

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "kinectecConverter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "kinectecConverter1");

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesDetectionConfiguration bodiesDetectionConfiguration = new BodiesDetectionConfiguration();
            bodiesDetectionConfiguration.SendBodiesDuringCalibration = true;
            bodiesDetectionConfiguration.DoCalibration = true;
            bodiesDetectionConfiguration.ConfidenceLevelForCalibration = Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            BodiesDetection.BodiesDetection bodiesDetection = new BodiesDetection.BodiesDetection(pipeline, bodiesDetectionConfiguration);

            /*** POSITION SELECTER ***/
            // Basic configuration for the moment.
            SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
            SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(pipeline, bodiesSelectionConfiguration);

            /*** INSTANT GROUPS ***/
            // Basic configuration for the moment.
            InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
            InstantGroups frameGroups = new InstantGroups(pipeline, instantGroupsConfiguration);

            /*** INTEGRATED GROUPS ***/
            // Basic configuration for the moment.
            IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
            IntegratedGroups intgratedGroups = new IntegratedGroups(pipeline, integratedGroupsConfiguration);

            /*** MORE TO COME ! ***/


            /*** LINKAGE ***/
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);
            bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
            //bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
            //positionExtraction.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
            //frameGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);


            // RunAsync the pipeline in non-blocking mode.
            pipeline.RunAsync();
            InitializeComponent();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Stop correctly the pipeline.
            pipeline.Dispose();
            base.OnClosing(e);
        }
    }
}

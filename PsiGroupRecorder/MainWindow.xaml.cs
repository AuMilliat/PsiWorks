using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.AzureKinect;
using BodyVisualizer;
using Bodies;

namespace PsiGroupsRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AzureKinectBodyVisualizer Visu0 { get; }
        public AzureKinectBodyVisualizer Visu1 { get; }

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

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "kinectecConverter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "kinectecConverter1");

            /*** REMOTE APPLICATION ***/
            RemoteImporter importer = new RemoteImporter(pipeline, ReplayDescriptor.ReplayAll.Interval, "localhost");
            if (!importer.Connected.WaitOne(-1))
            {
                throw new Exception("could not connect to server");
            }
            var group1 = importer.Importer.OpenStream<uint>("Group1");
            var group2 = importer.Importer.OpenStream<uint>("Group2");
            var group3 = importer.Importer.OpenStream<uint>("Group3");
            var group4 = importer.Importer.OpenStream<uint>("Group4");
            var group5 = importer.Importer.OpenStream<uint>("Group5");

            /*** BODIES VISUALIZERS ***/
            Visu0 = new AzureKinectBodyVisualizer(pipeline,null);
            Visu1 = new AzureKinectBodyVisualizer(pipeline,null);
            // Linkage
            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);

            //visu0
            sensor0.ColorImage.PipeTo(Visu0.InColorImage);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Visu0.InCalibration);
            bodiesConverter0.OutBodies.PipeTo(Visu0.InBodies);

            //converter1
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);

            //visu1
            sensor1.ColorImage.PipeTo(Visu1.InColorImage);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(Visu1.InCalibration);
            bodiesConverter1.OutBodies.PipeTo(Visu1.InBodies);

            /*** DATA STORING FOR PSI STUDIO ***/
            var store = PsiStore.Create(pipeline, "GroupsStoring", "F:\\Stores");
            store.Write(sensor0.Bodies, "Bodies0");
            store.Write(sensor1.Bodies, "Bodies1");
            store.Write(sensor0.DepthDeviceCalibrationInfo, "CalibBodies0");
            store.Write(sensor1.DepthDeviceCalibrationInfo, "CalibBodies1");
            store.Write(group1, "Group1");
            store.Write(group2, "Group2");
            store.Write(group3, "Group3");
            store.Write(group4, "Group4");
            store.Write(group5, "Group5");

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

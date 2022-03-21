﻿using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.AzureKinect;

namespace PsiGroupsRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Status { get; set; } = "Status: Offline";

        private readonly string StatusBase = "Status: ";

        public AzureKinectBodyTrackerVisualizer Visu0 { get; }
        public AzureKinectBodyTrackerVisualizer Visu1 { get; }

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

            /*** REMOTE APPLICATION ***/
            RemoteImporter importer = new RemoteImporter(pipeline, ReplayDescriptor.ReplayAll.Interval, "localhost");
            if (!importer.Connected.WaitOne(-1))
            {
                throw new Exception("could not connect to server");
            }
            Status = StatusBase + "Connected!";
            var group1 = importer.Importer.OpenStream<uint>("Group1");
            var group2 = importer.Importer.OpenStream<uint>("Group2");
            var group3 = importer.Importer.OpenStream<uint>("Group3");
            var group4 = importer.Importer.OpenStream<uint>("Group4");
            var group5 = importer.Importer.OpenStream<uint>("Group5");

            /*** BODIES VISUALIZERS ***/
            Visu0 = new AzureKinectBodyTrackerVisualizer(pipeline);
            Visu1 = new AzureKinectBodyTrackerVisualizer(pipeline);
            // Linkage
            sensor0.DepthDeviceCalibrationInfo.PipeTo(Visu0.CalibrationIn);
            sensor0.Bodies.PipeTo(Visu0.BodiesIn);
            sensor0.ColorImage.PipeTo(Visu0.ColorImageIn);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(Visu1.CalibrationIn);
            sensor1.Bodies.PipeTo(Visu1.BodiesIn);
            sensor1.ColorImage.PipeTo(Visu1.ColorImageIn);

            /*** DATA STORING FOR PSI STUDIO ***/
            var store = PsiStore.Create(pipeline, "GroupsStoring", "F:\\Stores");
            store.Write(sensor0.Bodies, "Bodies0");
            store.Write(sensor1.Bodies, "Bodies1");
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

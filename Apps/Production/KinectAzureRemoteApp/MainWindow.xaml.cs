using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;


namespace KinectAzureRemoteApp
{
  
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private string state = "Hello";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }
        public void DelegateMethodStatus(string status)
        {
            State = status;
        }

        private uint kinectIndex = 0;
        public uint KinectIndex
        {
            get => kinectIndex;
            set => SetProperty(ref kinectIndex, value);
        }
        public void DelegateMethodKinect(uint index)
        {
            KinectIndex = index;
        }

        private uint remotePort = 11411;
        public uint RemotePort
        {
            get => remotePort;
            set => SetProperty(ref remotePort, value);
        }
        public void DelegateMethodRemote(uint port)
        {
            RemotePort = port;
        }

        private Pipeline pipeline;
        public MainWindow()
        {
            DataContext = this;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create("WpfPipeline",enableDiagnostics: true);

            InitializeComponent();

        }

        private void PipelineSetup()
        {
            int portCount = 0;
            /*** KINECT SENSORS ***/
           // Only need Skeleton for the moment.
           AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
           configKinect.DeviceIndex = (int)KinectIndex;
           if(Skeleton.IsChecked == true )
                configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor = new AzureKinectSensor(pipeline, configKinect);

       
            TransportKind type = UDP.IsChecked == true ? TransportKind.Udp : TransportKind.Tcp;
            if (Sound.IsChecked == true)
            {
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(pipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                soundExporter.Exporter.Write(audioCapture.Out, "Kinect_"+ KinectIndex.ToString()+ "_Sound");
            }
            if (Skeleton.IsChecked == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                skeletonExporter.Exporter.Write(sensor.Bodies, "Kinect_" + KinectIndex.ToString() + "_Bodies"); 
                skeletonExporter.Exporter.Write(sensor.DepthDeviceCalibrationInfo, "Kinect_" + KinectIndex.ToString() + "_Calibration");
            }
            if (RGB.IsChecked == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                imageExporter.Exporter.Write(sensor.ColorImage.EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
            }
            if (Depth.IsChecked == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                depthExporter.Exporter.Write(sensor.DepthImage.EncodePng(), "Kinect_" + KinectIndex.ToString() + "_Depth");
            }
            pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            State = "Running";
        }

        private void StopPipeline()
        {
            // Stop correctly the pipeline.
            State = "Stopping";
            pipeline.Dispose();
            pipeline.WaitAll();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopPipeline();
            base.OnClosing(e);
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            StopPipeline();
            Close();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            PipelineSetup();
        }
    }
}

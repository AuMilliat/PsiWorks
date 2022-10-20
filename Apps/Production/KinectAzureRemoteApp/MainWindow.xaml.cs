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

            AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
            AudioCapture audioCapture = new AudioCapture(pipeline, configuration);
            if (Sound.IsChecked == true)
            {
                RemoteExporter soundExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, TransportKind.Tcp);
                soundExporter.Exporter.Write(audioCapture.Out, "Kinect_"+ KinectIndex.ToString()+ "_Sound");
            }
            if (Skeleton.IsChecked == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, TransportKind.Tcp);
                skeletonExporter.Exporter.Write(sensor.Bodies, "Kinect_" + KinectIndex.ToString() + "_Bodies"); 
                skeletonExporter.Exporter.Write(sensor.DepthDeviceCalibrationInfo, "Kinect_" + KinectIndex.ToString() + "_Calibration");
            }
            if (RGB.IsChecked == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, TransportKind.Tcp);
                imageExporter.Exporter.Write(sensor.ColorImage.EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
            }
            if (Depth.IsChecked == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, TransportKind.Tcp);
                depthExporter.Exporter.Write(sensor.DepthImage.EncodePng(), "Kinect_" + KinectIndex.ToString() + "_Depth");
            }
            pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            State = "Running";
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            // Stop correctly the pipeline.
            State = "Stopping";
            pipeline.Dispose();
            Close();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            PipelineSetup();
        }
    }
}

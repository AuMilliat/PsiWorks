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
using System;


namespace KinectAzureRemoteApp
{
    //public class Resolution
    //{
    //    public enum EResolution { Native, R1920_1080, R1280_800, R800_600 };

    //    public EResolution Id { get; set; } = EResolution.Native;
    //    public 
    //    public Resolution() { }

    //}

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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

        //ToDo add more resolution definition
        public enum Resolution{ Native, R1920_1080, R1280_720, R800_600 };
        private Dictionary<Resolution, Tuple<float, float>> resolutionDictionary;
        public List<Resolution> ResolutionsList { get; }


        private Resolution colorResolution = Resolution.Native;
        public Resolution ColorResolution
        {
            get => colorResolution;
            set => SetProperty(ref colorResolution, value);
        }
        public void DelegateMethodColorResolution(Resolution val)
        {
            ColorResolution = val;
        }

        //private Resolution depthResolution = Resolution.Native;
        //public Resolution DepthResolution
        //{
        //    get => depthResolution;
        //    set => SetProperty(ref depthResolution, value);
        //}
        //public void DelegateMethodDepthResolution(Resolution val)
        //{
        //    DepthResolution = val;
        //}

        private Pipeline pipeline;
        public MainWindow()
        {
            DataContext = this;
            resolutionDictionary = new Dictionary<Resolution, Tuple<float, float>>
            {
                 { Resolution.R1920_1080, new Tuple<float, float>(1920.0f, 1080.0f) }
                ,{ Resolution.R1280_720, new Tuple<float, float>(1280.0f, 720.0f) }
                ,{ Resolution.R800_600, new Tuple<float, float>(800.0f, 600.0f) }
            };
            ResolutionsList = new List<Resolution>();
            foreach (Resolution name in Enum.GetValues(typeof(Resolution)))
            {
                ResolutionsList.Add(name);
            }
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create("WpfPipeline", enableDiagnostics: true);

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
                if (colorResolution != Resolution.Native)
                {
                    Tuple<float, float> res = resolutionDictionary[colorResolution];
                    imageExporter.Exporter.Write(sensor.ColorImage.Resize(res.Item1, res.Item2).EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
                }
                else
                    imageExporter.Exporter.Write(sensor.ColorImage.EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
            }
            if (Depth.IsChecked == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);

                //if (depthResolution != Resolution.Native)
                //{
                //    Tuple<float, float> res = resolutionDictionary[colorResolution];
                //    depthExporter.Exporter.Write(sensor.DepthImage.EncodePng()., "Kinect_" + KinectIndex.ToString() + "_Depth");
                //}
                //else
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

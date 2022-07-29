using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Groups.Instant;
using Groups.Integrated;
using Groups.Entry;
using Bodies;
using CalibrationByBodies;
using NuitrackComponent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GroupsVisualizer;
using Postures;
using PosturesVisualizer;
using Microsoft.Psi.Calibration;
using Visualizer;


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


        public BasicVisualizer Visu0 { get; private set; }
        public BasicVisualizer Visu1 { get; private set; }
        //public BodyTrackerVisualizer.BodyTrackerVisualizer Visu0 { get; private set; }
        //public BodyTrackerVisualizer.BodyTrackerVisualizer Visu1 { get; private set; }
        public BodyCalibrationVisualizer.BodyCalibrationVisualizer Calib { get; private set; }
        public GroupsVisualizer.GroupsVisualizer InstantVisu { get; private set; }
        public GroupsVisualizer.GroupsVisualizer EntryVisu { get; private set; }
        public GroupsVisualizer.GroupsVisualizer IntegratedVisu { get; private set; }
        public PosturesVisualizer.PosturesVisualizer PosturesVisu { get; private set; }

        public DateTime stoppingTime { get; set; }
        private bool isPlaying = false;
        //private Microsoft.Psi.Data.PsiImporter? store = null;

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
            stoppingTime = new DateTime();
            DataContext = this;
            MathNet.Numerics.LinearAlgebra.Matrix<double> calibration;
            if (!Helpers.Helpers.ReadCalibrationFromFile("calib.csv", out calibration))
                calibration = null;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create(enableDiagnostics: true);
            Out = pipeline.CreateEmitter<bool>(this, nameof(this.Out));

            //PosturesPipeline();
            //KinectPipline(calibration);
            //KinectMonoPipline(calibration);
            //NuitrackPipline(calibration);
            StoreDisplayAndProcess(calibration);
            // RunAsync the pipeline in non-blocking mode.
            //pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            isPlaying = true;
            InitializeComponent();
            
        }

        private void StoreDisplayAndProcess(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
            var store = PsiStore.Open(pipeline, "GroupsStoring", "F:\\Stores\\2-2-1_5");
            var bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");
            var calib0 = store.OpenStream<IDepthDeviceCalibrationInfo>("CalibBodies0");
            var bodies1 = store.OpenStream<List<AzureKinectBody>>("Bodies1");
            var calib1 = store.OpenStream<IDepthDeviceCalibrationInfo>("CalibBodies1");

            //pipeline = store;
            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "converter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "converter1");

            /*** BODIES IDENTIFICATION ***/
            BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
            BodiesIdentification bodiesIdentification0 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);
            BodiesIdentification bodiesIdentification1 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesSelectionConfiguration bodiesSelectionConfiguration = new BodiesSelectionConfiguration();
            bodiesSelectionConfiguration.Camera2ToCamera1Transformation = calibration;
            BodiesSelection bodiesSelection = new BodiesSelection(pipeline, bodiesSelectionConfiguration);

            /*** BODIES DISPLAY ***/
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer0 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, null);
            Visu0 = bodyVisualizer0;
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer1 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, null);
            Visu1 = bodyVisualizer1;

            /*** Linkage ***/
            bodies0.PipeTo(bodiesConverter0.InBodiesAzure);
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

            bodies1.PipeTo(bodiesConverter1.InBodiesAzure);
            bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

            bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesSelection.InCamera1Bodies);
            bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesSelection.InCamera1LearnedBodies);

            bodiesIdentification1.OutBodiesIdentified.PipeTo(bodiesSelection.InCamera2Bodies);
            bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesSelection.InCamera2LearnedBodies);

            bodiesIdentification0.OutBodiesIdentified.PipeTo(bodyVisualizer0.InBodies);
            calib0.PipeTo(bodyVisualizer0.InCalibration);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(bodyVisualizer1.InBodies);
            calib1.PipeTo(bodyVisualizer1.InCalibration);


        }

        private void KinectPipline(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
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
            BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer visu0 = new BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            Visu0 = visu0;
            BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer visu1 = new BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            Visu1 = visu1;
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
            BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer calib = new BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer(pipeline, calibration);
            Calib = calib;

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
            bodiesDetectionConfiguration.Camera2ToCamera1Transformation = calibration;
            BodiesSelection bodiesDetection = new BodiesSelection(pipeline, bodiesDetectionConfiguration);

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

            /*** ENTRY GROUPS ***/
            // Basic configuration for the moment.
            EntryGroupsConfiguration entryGroupsConfiguration = new EntryGroupsConfiguration();
            EntryGroups entryGroups = new EntryGroups(pipeline, entryGroupsConfiguration);

            /*** POSTURES ***/
            // Basic configuration for the moment.
            //SimplePostures postures = new SimplePostures(pipeline);

            /*** STATS ***/
            //BodiesStatisticsConfiguration bodiesStatisticsConfiguration0 = new BodiesStatisticsConfiguration();
            //bodiesStatisticsConfiguration0.StoringPath = "F:/Stats/Kinect0Stats.csv";
            //BodiesStatistics stat0 = new BodiesStatistics(pipeline, bodiesStatisticsConfiguration0);
            //BodiesStatisticsConfiguration bodiesStatisticsConfiguration1 = new BodiesStatisticsConfiguration();
            //bodiesStatisticsConfiguration1.StoringPath = "F:/Stats/Kinect1Stats.csv";
            //BodiesStatistics stat1 = new BodiesStatistics(pipeline, bodiesStatisticsConfiguration1);
            //BodiesStatisticsConfiguration bodiesStatisticsConfiguration = new BodiesStatisticsConfiguration();
            //bodiesStatisticsConfiguration.StoringPath = "F:/Stats/KinectStats.csv";
            //BodiesStatistics stat = new BodiesStatistics(pipeline, bodiesStatisticsConfiguration);

            /*** Visualizers ! ***/
            GroupsVisualizerConfguration confifGroupsVisu = new GroupsVisualizerConfguration();
            AzureKinectGroupsVisualizer instantVisu = new AzureKinectGroupsVisualizer(pipeline, confifGroupsVisu);
            AzureKinectGroupsVisualizer entryVisu = new AzureKinectGroupsVisualizer(pipeline, confifGroupsVisu);
            AzureKinectGroupsVisualizer integratedVisu = new AzureKinectGroupsVisualizer(pipeline, confifGroupsVisu);
            AzureKinectPosturesVisualizer posturesVisualizer = new AzureKinectPosturesVisualizer(pipeline);

            InstantVisu = instantVisu;
            EntryVisu = entryVisu;
            IntegratedVisu = integratedVisu;
            PosturesVisu = posturesVisualizer;

            /*** LINKAGE ***/
            // Sensor0 -> Converter0 -> Identificator0 -> Visu0      |  
            //                                         -> Calibration -> Detector -> Extractor -> Instant -> Integrated
            // Sensor1 -> Converter1 -> Identificator1 -> Visu1      |-> VisuCalib                       |-> Entry

            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);

            //identificator0
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

            //visu0
            sensor0.ColorImage.PipeTo(visu0.InColorImage);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(visu0.InCalibration);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(visu0.InBodies);

            //converter1
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);

            //identificator1
            bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

            //visu1
            sensor1.ColorImage.PipeTo(visu1.InColorImage);
            sensor1.DepthDeviceCalibrationInfo.PipeTo(visu1.InCalibration);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(visu1.InBodies);

            //calib
            Out.PipeTo(calibrationByBodies.InSynchEvent);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera1Bodies);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera2Bodies);

            //detector
            calibrationByBodies.OutCalibration.PipeTo(bodiesDetection.InCalibrationMatrix);
            bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
            bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);
            bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesDetection.InCamera2LearnedBodies);

            //visucalib
            sensor0.DepthDeviceCalibrationInfo.PipeTo(calib.InCalibrationMaster);
            sensor0.ColorImage.PipeTo(Calib.InColorImage);
            calibrationByBodies.OutCalibration.PipeTo(Calib.InCalibrationSlave);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(Calib.InBodiesMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(Calib.InBodiesSlave);

            //extractor
            bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);

            //Instant
            positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);

            //integrated
            //instantGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);

            //entry
            //instantGroups.OutInstantGroups.PipeTo(entryGroups.InInstantGroups);

            //instantVisu
            //sensor0.DepthDeviceCalibrationInfo.PipeTo(instantVisu.InCalibration);
            //instantGroups.OutInstantGroups.PipeTo(instantVisu.InGroups);
            //bodiesDetection.OutBodiesCalibrated.PipeTo(instantVisu.InBodies);

            //entryVisu
            //sensor0.DepthDeviceCalibrationInfo.PipeTo(entryVisu.InCalibration);
            //entryGroups.OutFormedEntryGroups.PipeTo(entryVisu.InGroups);
            //bodiesDetection.OutBodiesCalibrated.PipeTo(entryVisu.InBodies);

            //integratedVisu
            sensor0.DepthDeviceCalibrationInfo.PipeTo(integratedVisu.InCalibration);
            intgratedGroups.OutIntegratedGroups.PipeTo(integratedVisu.InGroups);
            bodiesDetection.OutBodiesCalibrated.PipeTo(integratedVisu.InBodies);

            //postures
            //bodiesDetection.OutBodiesCalibrated.PipeTo(postures.InBodies);

            //posturesVisu
            //bodiesDetection.OutBodiesCalibrated.PipeTo(posturesVisualizer.InBodies);
            //sensor0.DepthDeviceCalibrationInfo.PipeTo(posturesVisualizer.InCalibration);
            //postures.OutPostures.PipeTo(posturesVisualizer.InPostures);

            //Stats
            //bodiesConverter0.OutBodies.PipeTo(stat0.InBodies);
            //bodiesConverter1.OutBodies.PipeTo(stat1.InBodies);
            //bodiesDetection.OutBodiesCalibrated.PipeTo(stat.InBodies);
        }

        private void KinectMonoPipline(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
            /*** KINECT SENSOR ***/
            AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
            configKinect0.DeviceIndex = 0;
            configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor0 = new AzureKinectSensor(pipeline, configKinect0);

            /*** BODIES VISUALIZERS ***/
            BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer visu0 = new BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            Visu0 = visu0;
         

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "kinectecConverter0");

            /*** BODIES IDENTIFICATION ***/
            BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
            BodiesIdentification bodiesIdentification0 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);
            
            /*** LINKAGE ***/
            // Sensor0 -> Converter0 -> Identificator0 -> Visu0

            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
            //identificator0
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);
            //visu0
            sensor0.ColorImage.PipeTo(visu0.InColorImage);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(visu0.InCalibration);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(visu0.InBodies);
        }

        private void NuitrackPipline(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
            //NOT WORKING AS YOU CAN HAVE ONLY ONE DEVICE PER PROCESS
            //IT SHOULD BE USED WITH REMOTE EXP/IMPOTER

            /*** NUITRACK SENSOR ***/
            NuitrackCoreConfiguration configNui0 = new NuitrackCoreConfiguration();
            configNui0.DeviceIndex = 0;
            configNui0.ActivationKey = "license:35365:LmoTHY7vt5v2Q1A5";
            NuitrackSensor sensor0 = new NuitrackSensor(pipeline, configNui0);

            NuitrackCoreConfiguration configNui1 = new NuitrackCoreConfiguration();
            configNui1.DeviceIndex = 1;
            configNui1.ActivationKey = "license:34821:ZvAVGW03StUh056F";
            NuitrackSensor sensor1 = new NuitrackSensor(pipeline, configNui1);

            /*** BODIES VISUALIZERS ***/
            BodyTrackerVisualizer.NuitrackBodyTrackerVisualizer visu0 = new BodyTrackerVisualizer.NuitrackBodyTrackerVisualizer(pipeline, sensor0);
            Visu0 = Visu0;
            BodyTrackerVisualizer.NuitrackBodyTrackerVisualizer visu1 = new BodyTrackerVisualizer.NuitrackBodyTrackerVisualizer(pipeline, sensor1);
            Visu1 = Visu1;

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "nuitrackConverter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "nuitrackConverter1");

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
            Calib = new BodyCalibrationVisualizer.NuitrackBodyCalibrationVisualizer(pipeline, sensor0, null);


            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
            bodiesDetectionConfiguration.Camera2ToCamera1Transformation = calibration;
            BodiesSelection bodiesDetection = new BodiesSelection(pipeline, bodiesDetectionConfiguration);
            
            ///*** POSITION SELECTER ***/
            //// Basic configuration for the moment.
            //SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
            //SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(pipeline, bodiesSelectionConfiguration);
            //
            ///*** INSTANT GROUPS ***/
            //// Basic configuration for the moment.
            //InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
            //InstantGroups instantGroups = new InstantGroups(pipeline, instantGroupsConfiguration);
            //
            ///*** INTEGRATED GROUPS ***/
            //// Basic configuration for the moment.
            //IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
            //IntegratedGroups intgratedGroups = new IntegratedGroups(pipeline, integratedGroupsConfiguration);

            /*** MORE TO COME ! ***/


            /*** LINKAGE ***/
            // Sensor0 -> Converter0 -> Identificator0 -> Visu0      |  
            //                                         -> Calibration -> Detector -> Extractor -> Instant -> Integrated
            // Sensor1 -> Converter1 -> Identificator1 -> Visu1      |-> VisuCalib                       |-> Entry

            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesNuitrack);
            //identificator0

            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);
            //visu0
            sensor0.ColorImage.PipeTo(visu0.InColorImage);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(visu0.InBodies);

            //converter1
            sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesNuitrack);

            //identificator1
            bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

            //visu1
            sensor1.ColorImage.PipeTo(visu1.InColorImage);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(visu1.InBodies);

            //calib
            Out.PipeTo(calibrationByBodies.InSynchEvent);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera1Bodies);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calibrationByBodies.InCamera2Bodies);

            //detector
            //calibrationByBodies.OutCalibration.PipeTo(bodiesDetection.InCalibrationMatrix);
            //bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            //bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);

            //visucalib
            sensor0.ColorImage.PipeTo(Calib.InColorImage);
            calibrationByBodies.OutCalibration.PipeTo(Calib.InCalibrationSlave);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(Calib.InBodiesMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(Calib.InBodiesSlave);

            ////extractor
            //bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
            //
            ////Instant
            //positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);
            //
            ////integrated
            //instantGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);
        }

        private void PosturesPipeline()
        {
            /*** KINECT SENSOR ***/
            AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
            configKinect0.DeviceIndex = 0;
            configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor0 = new AzureKinectSensor(pipeline, configKinect0);

            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline);

            /*** BODIES IDENTIFICATION ***/
            BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
            BodiesIdentification bodiesIdentification0 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration);

            BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer visu0 = new BodyTrackerVisualizer.AzureKinectBodyTrackerVisualizer(pipeline);
            Visu0 = visu0;

            /*** POSTURES ***/
            // Basic configuration for the moment.
            SimplePostures postures = new SimplePostures(pipeline);

            /*** Visualizer ! ***/
            AzureKinectPosturesVisualizer posturesVisualizer = new AzureKinectPosturesVisualizer(pipeline);

            PosturesVisu = posturesVisualizer;

            /*** LINKAGE ***/
            // Sensor0 -> Converter0 -> Identification -> Visu0     |  
            //                                         -> Postures -> VisuPostures

            //converter0
            sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);

            //identificator0
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

            //visu0
            sensor0.ColorImage.PipeTo(visu0.InColorImage);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(visu0.InCalibration);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(visu0.InBodies);

            //postures
            bodiesConverter0.OutBodies.PipeTo(postures.InBodies);

            //posturesVisu
            bodiesConverter0.OutBodies.PipeTo(posturesVisualizer.InBodies);
            sensor0.DepthDeviceCalibrationInfo.PipeTo(posturesVisualizer.InCalibration);
            sensor0.ColorImage.PipeTo(posturesVisualizer.InColorImage);
            postures.OutPostures.PipeTo(posturesVisualizer.InPostures);
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
            {
                pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            }
        }
    }
}

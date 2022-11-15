using Bodies;
using CalibrationByBodies;
using Groups;
using GroupsVisualizer;
using GroundTruthGroups;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Calibration;
using NuitrackComponent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Visualizer;
using System.Windows.Documents;


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

    public class Watch : IProgress<double>
    {
        private readonly MainWindow App;

        public Watch(MainWindow app)
        {
            App = app;
        }
        public void Report(double value)
        {
       
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
        public BasicVisualizer Visu2 { get; private set; }
        public BasicVisualizer Visu3 { get; private set; }
        public BasicVisualizer Visu4 { get; private set; }
        public BasicVisualizer Visu5 { get; private set; }

        public TruthCentralizer Truth { get; private set; }


        private Watch progress;
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
            progress = new Watch(this);
            DataContext = this;
            MathNet.Numerics.LinearAlgebra.Matrix<double> calibration;
            if (!Helpers.Helpers.ReadCalibrationFromFile("calib.csv", out calibration))
                calibration = null;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create("WpfPipeline",enableDiagnostics: true);
            Out = pipeline.CreateEmitter<bool>(this, nameof(this.Out));

            //StoreDisplayAndProcess(calibration);
            NuitrackPipline(calibration);
            //InitializeComponent();

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
            BodyVisualizer.AzureKinectBodyVisualizer visu0 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, null);
            Visu0 = visu0;
            BodyVisualizer.AzureKinectBodyVisualizer visu1 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, null);
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

            /*** Configuration for all visualizer ***/
            BasicVisualizerConfiguration visualizerConfiguration = new BasicVisualizerConfiguration();
            visualizerConfiguration.WithVideoStream = true;

            /*** BODIES DISPLAY ***/
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer0 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, visualizerConfiguration);
            Visu0 = bodyVisualizer0;
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer1 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, visualizerConfiguration);
            Visu1 = bodyVisualizer1;

            /*** CALIBRATION VISUALIZER ***/
            BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer calib = new BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer(pipeline, visualizerConfiguration, false);
            Visu2 = calib;

            /*** CALIBRATION STATISTICS ***/
            CalibrationStatisticsConfiguration calibconfig = new CalibrationStatisticsConfiguration();
            calibconfig.CalculationType = CalibrationStatisticsConfiguration.TestingType.ByNumberOfFrames;
            calibconfig.TestingCount = 1;
            CalibrationStatistics calibStat = new CalibrationStatistics(pipeline, calibconfig);

            /*** LINKAGE ***/
            // Sensor0 -> Converter0 -> Identificator0 -> Visu0      |-> StatCalib
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

            ////detector
            //calibrationByBodies.OutCalibration.PipeTo(bodiesDetection.InCalibrationMatrix);
            //bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
            //bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
            //bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);
            //bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesDetection.InCamera2LearnedBodies);

            //visucalib
            sensor0.DepthDeviceCalibrationInfo.PipeTo(calib.InCalibrationMaster);
            sensor0.ColorImage.PipeTo(calib.InColorImage);
            calibrationByBodies.OutCalibration.PipeTo(calib.InCalibrationSlave);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calib.InBodiesMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calib.InBodiesSlave);

            //calibstat
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calibStat.InCamera1Bodies);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calibStat.InCamera2Bodies);
            calibrationByBodies.OutCalibration.PipeTo(calibStat.InCalibrationMatrix);
            Out.PipeTo(calibrationByBodies.InSynchEvent);

            ////extractor
            //bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);

            ////Instant
            //positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);

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
            //sensor0.DepthDeviceCalibrationInfo.PipeTo(integratedVisu.InCalibration);
            //intgratedGroups.OutIntegratedGroups.PipeTo(integratedVisu.InGroups);
            //bodiesDetection.OutBodiesCalibrated.PipeTo(integratedVisu.InBodies);

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

        private void StoreDisplayAndProcess(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
            var store = PsiStore.Open(pipeline, "GroupsStoring", "F:\\Stores\\2-2-1");
            var bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");
            var bodies1 = store.OpenStream<List<AzureKinectBody>>("Bodies1");

            var calib0 = store.OpenStream<IDepthDeviceCalibrationInfo>("CalibBodies0");
            var calib1 = store.OpenStream<IDepthDeviceCalibrationInfo>("CalibBodies1");

            var group1 = store.OpenStream<uint>("Group1");
            var group2 = store.OpenStream<uint>("Group2");
            var group3 = store.OpenStream<uint>("Group3");
            var group4 = store.OpenStream<uint>("Group4");
            var group5 = store.OpenStream<uint>("Group5");

            //pipeline = store;
            /*** BODIES CONVERTERS ***/
            BodiesConverter bodiesConverter0 = new BodiesConverter(pipeline, "converter0");
            BodiesConverter bodiesConverter1 = new BodiesConverter(pipeline, "converter1");

            /*** BODIES IDENTIFICATION ***/
            BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
            //bodiesIdentificationConfiguration.PostLearnedOnly = true;
            BodiesIdentification bodiesIdentification0 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration, "0");
            BodiesIdentification bodiesIdentification1 = new BodiesIdentification(pipeline, bodiesIdentificationConfiguration, "1");

            /*** BODIES DETECTION ***/
            // Basic configuration for the moment.
            BodiesSelectionConfiguration bodiesSelectionConfiguration = new BodiesSelectionConfiguration();
            bodiesSelectionConfiguration.Camera2ToCamera1Transformation = calibration;
            BodiesSelection bodiesSelection = new BodiesSelection(pipeline, bodiesSelectionConfiguration);

            /*** Configuration for all visualizer ***/
            BasicVisualizerConfiguration visualizerConfiguration = new BasicVisualizerConfiguration();
            visualizerConfiguration.WithVideoStream = false;

            /*** BODIES DISPLAY ***/
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer0 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, visualizerConfiguration);
            //Visu0 = bodyVisualizer0;
            BodyVisualizer.AzureKinectBodyVisualizer bodyVisualizer1 = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, visualizerConfiguration);
            //Visu1 = bodyVisualizer1;

            /*** CALIBRATION VISUALIZER ***/
            BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer calib = new BodyCalibrationVisualizer.AzureKinectBodyCalibrationVisualizer(pipeline, visualizerConfiguration, false);
            calib.Calibration = calibration;
            //Visu2 = calib;

            /*** SELECTION VISUALIZER ***/
            BodyVisualizer.AzureKinectBodyVisualizer selectionVisualizer = new BodyVisualizer.AzureKinectBodyVisualizer(pipeline, visualizerConfiguration);
            Visu2 = selectionVisualizer;
            //Visu3 = selectionVisualizer;

            /*** POSITION EXTRACTOR ***/
            SimpleBodiesPositionExtractionConfiguration PEConfiguration = new SimpleBodiesPositionExtractionConfiguration();
            SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(pipeline, PEConfiguration);

            /*** INSTANT GROUPS ***/
            InstantGroupsConfiguration IGConfiguration = new InstantGroupsConfiguration();
            InstantGroups instantGroups = new InstantGroups(pipeline, IGConfiguration);

            /* INSTANT GROUPS VISUALIZER */
            AzureKinectGroupsVisualizer azureKinectInstantGroupsVisualizer = new AzureKinectGroupsVisualizer(pipeline, visualizerConfiguration);
            //Visu4 = azureKinectInstantGroupsVisualizer;
            Visu3 = azureKinectInstantGroupsVisualizer;

            /*** INTEGRATED GROUPS ***/
            IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
            IntegratedGroups integratedGroups = new IntegratedGroups(pipeline, integratedGroupsConfiguration);

            /* INTEGRATED GROUPS VISUALIZER */
            AzureKinectGroupsVisualizer azureKinectIntegratedGroupsVisualizer = new AzureKinectGroupsVisualizer(pipeline, visualizerConfiguration);
            Visu5 = azureKinectIntegratedGroupsVisualizer;

            /*** ENTRY GROUPS ***/
            EntryGroupsConfiguration entryGroupsConfiguration = new EntryGroupsConfiguration();
            EntryGroups entryGroups = new EntryGroups(pipeline, entryGroupsConfiguration);

            /* ENTRY GROUPS VISUALIZER */
            AzureKinectGroupsVisualizer azureKinectEntryGroupsVisualizer = new AzureKinectGroupsVisualizer(pipeline, visualizerConfiguration);
            Visu4 = azureKinectEntryGroupsVisualizer;

            /*** GROUND TRUTH GROUPS ***/
            Truth = new TruthCentralizer(pipeline);

            /*** Linkage ***/
            bodies0.PipeTo(bodiesConverter0.InBodiesAzure);
            bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

            bodies1.PipeTo(bodiesConverter1.InBodiesAzure);
            bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

            bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesSelection.InCamera1Bodies);
            bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesSelection.InCamera1LearnedBodies);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calib.InBodiesMaster);

            bodiesIdentification1.OutBodiesIdentified.PipeTo(bodiesSelection.InCamera2Bodies);
            bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesSelection.InCamera2LearnedBodies);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calib.InBodiesSlave);

            bodiesIdentification0.OutBodiesIdentified.PipeTo(bodyVisualizer0.InBodies);
            calib0.PipeTo(bodyVisualizer0.InCalibration);
            calib0.PipeTo(calib.InCalibrationMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(bodyVisualizer1.InBodies);
            calib1.PipeTo(bodyVisualizer1.InCalibration);

            bodiesSelection.OutBodiesCalibrated.PipeTo(selectionVisualizer.InBodies);
            calib0.PipeTo(selectionVisualizer.InCalibration);

            bodiesSelection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
            positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);

            instantGroups.OutInstantGroups.PipeTo(azureKinectInstantGroupsVisualizer.InGroups);
            bodiesSelection.OutBodiesCalibrated.PipeTo(azureKinectInstantGroupsVisualizer.InBodies);
            calib0.PipeTo(azureKinectInstantGroupsVisualizer.InCalibration);

            bodiesSelection.OutBodiesRemoved.PipeTo(integratedGroups.InRemovedBodies);
            instantGroups.OutInstantGroups.PipeTo(integratedGroups.InInstantGroups);
            integratedGroups.OutIntegratedGroups.PipeTo(azureKinectIntegratedGroupsVisualizer.InGroups);
            bodiesSelection.OutBodiesCalibrated.PipeTo(azureKinectIntegratedGroupsVisualizer.InBodies);
            calib0.PipeTo(azureKinectIntegratedGroupsVisualizer.InCalibration);

            bodiesSelection.OutBodiesRemoved.PipeTo(entryGroups.InRemovedBodies);
            instantGroups.OutInstantGroups.PipeTo(entryGroups.InInstantGroups);
            entryGroups.OutFormedEntryGroups.PipeTo(azureKinectEntryGroupsVisualizer.InGroups);
            bodiesSelection.OutBodiesCalibrated.PipeTo(azureKinectEntryGroupsVisualizer.InBodies);
            calib0.PipeTo(azureKinectEntryGroupsVisualizer.InCalibration);

            group1.PipeTo(Truth.InGroup1);
            group2.PipeTo(Truth.InGroup2);
            group3.PipeTo(Truth.InGroup3);
            group4.PipeTo(Truth.InGroup4);
            group5.PipeTo(Truth.InGroup5);
        }

        private void NuitrackPipline(MathNet.Numerics.LinearAlgebra.Matrix<double> calibration)
        {
            /*** NUITRACK SENSOR ***/
            NuitrackCoreConfiguration configNui0 = new NuitrackCoreConfiguration();
            configNui0.DeviceIndex = 0;
            configNui0.ActivationKey = "license:6612:V8X39p8018x11uTZ";
            NuitrackSensor sensor0 = new NuitrackSensor(pipeline, configNui0);

            NuitrackCoreConfiguration configNui1 = new NuitrackCoreConfiguration();
            configNui1.DeviceIndex = 1;
            configNui1.ActivationKey = "license:34821:ZvAVGW03StUh056F";
            NuitrackSensor sensor1 = new NuitrackSensor(pipeline, configNui1);

            /*** BODIES VISUALIZERS ***/
            BodyVisualizer.NuitrackBodyVisualizer visu0 = new BodyVisualizer.NuitrackBodyVisualizer(pipeline, sensor0, null);
            Visu0 = Visu0;
            BodyVisualizer.NuitrackBodyVisualizer visu1 = new BodyVisualizer.NuitrackBodyVisualizer(pipeline, sensor1, null);
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
            BodyCalibrationVisualizer.NuitrackBodyCalibrationVisualizer calib = new BodyCalibrationVisualizer.NuitrackBodyCalibrationVisualizer(pipeline, sensor0, null);
            Visu2 = calib;

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
            sensor0.ColorImage.PipeTo(calib.InColorImage);
            calibrationByBodies.OutCalibration.PipeTo(calib.InCalibrationSlave);
            bodiesIdentification0.OutBodiesIdentified.PipeTo(calib.InBodiesMaster);
            bodiesIdentification1.OutBodiesIdentified.PipeTo(calib.InBodiesSlave);

            ////extractor
            //bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
            //
            ////Instant
            //positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);
            //
            ////integrated
            //instantGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);
        }

        protected override void OnClosing(CancelEventArgs e)
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
                pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime, progress);
            }
        }
    }
}

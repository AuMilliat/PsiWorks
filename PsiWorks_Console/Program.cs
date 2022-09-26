using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.AzureKinect;
using NuitrackComponent;
using Groups;
using Bodies;
using Postures;
using NatNetComponent;
using LabJackComponent;
using LabJack.LabJackUD;
using Tobii;
using System.Xml.Linq;

internal sealed class KeyboardReader : Microsoft.Psi.Components.ISourceComponent, IProducer<string>
{
    public Emitter<string> Out { get; private set; }
    private Thread? captureThread = null;
    private bool shutdown = false;

    public KeyboardReader(Pipeline pipeline)
    {
       //this.Out = pipeline.CreateEmitter<string>(this, ServerDataStream);
       //PAS BON ->
       this.Out = pipeline.CreateEmitter<string>(this, nameof(this.Out));
    }

    public void Start(Action<DateTime> notifyCompletionTime)
    {
        // notify that this is an infinite source component
        notifyCompletionTime(DateTime.MaxValue);
        this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
        this.captureThread.Start();
    }

    public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
    {
        shutdown = true;
        TimeSpan waitTime = TimeSpan.FromSeconds(1);
        if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
        {
#pragma warning disable SYSLIB0006 // Le type ou le membre est obsolète
            captureThread.Abort();
#pragma warning restore SYSLIB0006 // Le type ou le membre est obsolète
        }
        notifyCompleted();
    }

    private void CaptureThreadProc()
    {
        while (!this.shutdown)
        {
            Console.WriteLine("Ready to send text!");
            var message = Console.ReadLine();
            if (message != null)
            {
                try
                {
                    Out.Post(message, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
class Program
{
    static void GroupsNuitrackTesting(Pipeline p)
    {
        /*** NUITRACK SENSOR ***/
        // Only need Skeleton for the moment.
        NuitrackCoreConfiguration configNuitrack = new NuitrackCoreConfiguration();
        configNuitrack.ActivationKey = "license:34821:ZvAVGW03StUh056F";
        //configNuitrack.OutputColor = false;
        //configNuitrack.OutputDepth = false;
        NuitrackSensor sensor = new NuitrackSensor(p, configNuitrack);

        /*** BODY DETECTOR ***/
        // Basic configuration for the moment.
        SimpleBodiesPositionExtractionConfiguration BodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
        SimpleBodiesPositionExtraction BodiesSelection = new SimpleBodiesPositionExtraction(p, BodiesSelectionConfiguration);

        /*** INSTANT GROUPS ***/
        // Basic configuration for the moment.
        InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
        InstantGroups frameGroups = new InstantGroups(p, instantGroupsConfiguration);

        /*** INTEGRATED GROUPS ***/
        // Basic configuration for the moment.
        IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
        IntegratedGroups intgratedGroups = new IntegratedGroups(p, integratedGroupsConfiguration);

        /*** MORE TO COME ! ***/


        /*** Linkage ***/
        sensor.Bodies.PipeTo(BodiesSelection.InBodiesNuitrack);
        BodiesSelection.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
        frameGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);

        /*** DATA STORING FOR PSI STUDIO ***/
        //var store = PsiStore.Create(p, "GroupsStoring", Path.Combine(Directory.GetCurrentDirectory(), "Stores"));
        //store.Write(sensor.ColorImage, "Image");
        //store.Write(frameGroups.OutInstantGroups, "InstantGroups");
        //store.Write(intgratedGroups.OutIntegratedGroups, "IntegratedGroups");
    }
    static void GroupsAzureTesting(Pipeline p)
    {
        /*** KINECT SENSORS ***/
        // Only need Skeleton for the moment.
        AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
        configKinect0.DeviceIndex = 0;
        configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor0 = new AzureKinectSensor(p, configKinect0);

        AzureKinectSensorConfiguration configKinect1 = new AzureKinectSensorConfiguration();
        configKinect1.DeviceIndex = 1;
        configKinect1.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor1 = new AzureKinectSensor(p, configKinect1);

        /*** BODIES CONVERTERS ***/
        BodiesConverter bodiesConverter0 = new BodiesConverter(p, "kinectecConverter0");
        BodiesConverter bodiesConverter1 = new BodiesConverter(p, "kinectecConverter1");

        /*** BODIES DETECTION ***/
        // Basic configuration for the moment.
        BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
        BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

        /*** POSITION SELECTER ***/
        // Basic configuration for the moment.
        SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
        SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(p, bodiesSelectionConfiguration);

        /*** INSTANT GROUPS ***/
        // Basic configuration for the moment.
        InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
        InstantGroups frameGroups = new InstantGroups(p, instantGroupsConfiguration);

        /*** INTEGRATED GROUPS ***/
        // Basic configuration for the moment.
        IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
        IntegratedGroups intgratedGroups = new IntegratedGroups(p, integratedGroupsConfiguration);

        /*** MORE TO COME ! ***/


        /*** LINKAGE ***/
        sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
        sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);
        bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
        bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
        bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
        positionExtraction.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
        frameGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);

    }

    static void SingleKinectSkeletonsRecording(Pipeline p)
    {
        /*** KINECT SENSOR ***/
        // Only need Skeleton for the moment.
        AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
        configKinect0.DeviceIndex = 0;
        configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor0 = new AzureKinectSensor(p, configKinect0);

        /*** DATA STORING FOR PSI STUDIO ***/
        var store = PsiStore.Create(p, "SingleKinectSkeletonsRecording", "F:\\Stores");
        //store.Write(sensor0.ColorImage, "Color0");
        store.Write(sensor0.DepthDeviceCalibrationInfo, "DepthCalibration");
        store.Write(sensor0.Bodies, "Bodies");
    }

    static void GroupSinleIdentificationTesting(Pipeline p)
    {
        var store = PsiStore.Open(p, "GroupsStoring", "F:\\Stores\\Free2");
        var Bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");

        /*** BODIES CONVERTERS ***/
        BodiesConverter bodiesConverter0 = new BodiesConverter(p, "converter0");

        /*** BODIES IDENTIFICATION ***/
        BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
        BodiesIdentification bodiesIdentification0 = new BodiesIdentification(p, bodiesIdentificationConfiguration);

        /*** BODIES DETECTION ***/
        // Basic configuration for the moment.
        BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();

        BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

        /*** Linkage ***/
        Bodies0.PipeTo(bodiesConverter0.InBodiesAzure);

        bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

        bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera1Bodies);
        bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);

    }

    static void GroupsUsingRecords(Pipeline p)
    {
        MathNet.Numerics.LinearAlgebra.Matrix<double>? calibration;
        if (!Helpers.Helpers.ReadCalibrationFromFile("calib.csv", out calibration))
            calibration = null;

        var store = PsiStore.Open(p, "GroupsStoring", "F:\\Stores\\Free2");
        var Bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");
        var Bodies1 = store.OpenStream<List<AzureKinectBody>>("Bodies1");

        /*** BODIES CONVERTERS ***/
        BodiesConverter bodiesConverter0 = new BodiesConverter(p, "converter0");
        BodiesConverter bodiesConverter1 = new BodiesConverter(p, "converter1");

        /*** BODIES IDENTIFICATION ***/
        BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
        BodiesIdentification bodiesIdentification0 = new BodiesIdentification(p, bodiesIdentificationConfiguration);
        BodiesIdentification bodiesIdentification1 = new BodiesIdentification(p, bodiesIdentificationConfiguration);

        /*** BODIES DETECTION ***/
        // Basic configuration for the moment.
        BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
        bodiesDetectionConfiguration.Camera2ToCamera1Transformation = calibration;
        BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

        /*** POSITION SELECTER ***/
        // Basic configuration for the moment.
        SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
        SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(p, bodiesSelectionConfiguration);

        /*** INSTANT GROUPS ***/
        // Basic configuration for the moment.
        InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
        InstantGroups instantGroups = new InstantGroups(p, instantGroupsConfiguration);

        /*** MORE TO COME ! ***/


        /*** Linkage ***/
        Bodies0.PipeTo(bodiesConverter0.InBodiesAzure);
        Bodies1.PipeTo(bodiesConverter1.InBodiesAzure);

        bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);
        bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

        bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera1Bodies);
        bodiesIdentification1.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera2Bodies);
        bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);
        bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesDetection.InCamera2LearnedBodies);

        bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
        positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);
    }

    static void HololensImporter(Pipeline p)
    {
        /*** REMOTE IMPORTER ! ***/
        RemoteImporter importer = new RemoteImporter(p, ReplayDescriptor.ReplayAll.Interval, "10.44.193.153");
        Console.WriteLine("connecting");
        if (!importer.Connected.WaitOne(-1))
        {
            throw new Exception("could not connect to server");
        }
        Console.WriteLine("connected");
    }

    static void LabJackNatNetTesting(Pipeline p)
    {
        /*** LABJACK ***/
        LabJackCoreConfiguration ljConfiguration = new LabJackCoreConfiguration();
        Commands commands = new Commands();
        commands.PutCommands = new List<PutCommand>();
        commands.RequestCommands = new List<RequestCommand>();
        commands.ResponseCommand.GetterType = ResponseCommand.EGetterType.First_Next;

        PutCommand putCommand = new PutCommand();
        putCommand.IoType = LJUD.IO.PUT_CONFIG;
        putCommand.Channel =LJUD.CHANNEL.AIN_RESOLUTION;
        putCommand.Val = 0;
        putCommand.X1 = new byte[1];

        RequestCommand requestCommand = new RequestCommand();
        requestCommand.IoType = LJUD.IO.GET_AIN;
        requestCommand.Channel = LJUD.CHANNEL.IP_ADDRESS;
        requestCommand.Val = 2;
        requestCommand.X1 = 0;
        requestCommand.UserData = 0;

        commands.PutCommands.Add(putCommand);
        commands.RequestCommands.Add(requestCommand);

        ljConfiguration.Commands = commands;

        LabJackSensor labJack = new LabJackSensor(p, ljConfiguration);

        /*** NATNET ***/
        //NatNetCoreConfiguration nnConfiguration = new NatNetCoreConfiguration();
        //NatNetSensor natNetSensor = new NatNetSensor(p, nnConfiguration);

        /*** DATA STORING FOR PSI STUDIO ***/
        var store = PsiStore.Create(p, "VetoStoring", "F:\\Stores");        
        store.Write(labJack.OutDoubleValue, "Sensor");
     //   store.Write(natNetSensor.OutRigidBodies, "OptiTrack");
    }


    static void KinectVideoSoundTesting(Pipeline p)
    {
        AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        configKinect.DeviceIndex = 0;
        configKinect.ColorResolution = Microsoft.Azure.Kinect.Sensor.ColorResolution.R720p;
        configKinect.CameraFPS = Microsoft.Azure.Kinect.Sensor.FPS.FPS30;
        // 
        // configKinect.ColorFormat = Microsoft.Azure.Kinect.Sensor.ImageFormat.ColorMJPG;  

        //configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        //AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

        //Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();
        //configuration.OptimizeForSpeech = true;
        //Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(p, configuration);

        //var store = PsiStore.Create(p, "KinectAudioStoring", "F:\\Stores");
        //store.Write(sensor.ColorImage, "Image");
        //store.Write(sensor.DepthDeviceCalibrationInfo, "DepthCalibration");
        //store.Write(sensor.Bodies, "Bodies");
        //store.Write(audioCapture.Out, "Audio");
    }


    static void HTCSoundTesting(Pipeline p)
    {

        //Console.WriteLine("audio");
        //foreach (string name in Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices())
        //    Console.WriteLine(name);


        //Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();
        //configuration.OptimizeForSpeech = true;
        //Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(p, configuration);
        
        ////Microsoft.Psi.Media.MediaCaptureConfiguration cfg = new Microsoft.Psi.Media.MediaCaptureConfiguration();

        //Microsoft.Psi.Media.MediaCapture capture = new Microsoft.Psi.Media.MediaCapture(p);


        //var store = PsiStore.Create(p, "HTCStoring", "D:\\Stores");

        //store.Write(capture.Audio, "Audio");
        //store.Write(capture.Video, "Video");
    }

    static void KinectPostures(Pipeline p)
    {
        AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        configKinect.DeviceIndex = 0;
        configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

        BodiesConverter bodiesConverter = new BodiesConverter(p);

        //SimplePostuesConfiguration configuration = new SimplePostuesConfiguration();
        //SimplePostures postures = new SimplePostures(p, configuration);

        BodiesStatisticsConfiguration bsConfguration = new BodiesStatisticsConfiguration();
        BodiesStatistics statistics = new BodiesStatistics(p, bsConfguration);

        sensor.Bodies.PipeTo(bodiesConverter.InBodiesAzure);
        bodiesConverter.OutBodies.PipeTo(statistics.InBodies);
    }

    static void testTobii(Pipeline p)
    {
        TobiiSensor sensor = new TobiiSensor(p);

    }

    static void Main(string[] args)
    {
        // Enabling diagnotstics !!!
        Pipeline p = Pipeline.Create(enableDiagnostics: true);

        //LabJackNatNetTesting(p);
        //KinectVideoSoundTesting(p);
        //KinectPostures(p);
        //HTCSoundTesting(p);

        /*** GROUPS TESTING ***/
        //GroupsTesting(p);
        // GroupsAzureTesting(p);
        //SingleKinectSkeletonsRecording(p); 

        /*** Record Groups ***/
        //GroupsRecording(p);
        testTobii(p);
        //GroupSinleIdentificationTesting(p);
        //GroupsUsingRecords(p);

        /*** HOLOLENS ***/
        //HololensImporter(p);

        // RunAsync the pipeline in non-blocking mode.
        p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
        // Wainting for an out key
        Console.WriteLine("Press any key to stop the application.");
        Console.ReadLine();
        // Stop correctly the pipeline.
        p.Dispose();
    }
}
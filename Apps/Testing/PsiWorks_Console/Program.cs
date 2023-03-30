using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Media;
//using Microsoft.Psi.AzureKinect;
using NuitrackComponent;
using Groups;
using Bodies;
using Postures;
using NatNetComponent;
using LabJackComponent;
using LabJack.LabJackUD;
using Tobii;
using System.Xml.Linq;
using RemoteConnectors;
using OpenFaceComponents;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Audio;
using WebRTC;
using Microsoft.Psi.Imaging;
using Emgu.CV.PpfMatch3d;
using System.Windows.Media.Animation;
using TinyJson;
using Microsoft.Psi.Data;
using KeyboardReader;
using Biopac;
using SharpDX;

class Program
{
    static void GroupsNuitrackTesting(Pipeline p)
    {
        /*** NUITRACK SENSOR ***/
        // Only need Skeleton for the moment.
        NuitrackSensorConfiguration configNuitrack = new NuitrackSensorConfiguration();
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
    //static void GroupsAzureTesting(Pipeline p)
    //{
    //    /*** KINECT SENSORS ***/
    //    // Only need Skeleton for the moment.
    //    AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
    //    configKinect0.DeviceIndex = 0;
    //    configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
    //    AzureKinectSensor sensor0 = new AzureKinectSensor(p, configKinect0);

    //    AzureKinectSensorConfiguration configKinect1 = new AzureKinectSensorConfiguration();
    //    configKinect1.DeviceIndex = 1;
    //    configKinect1.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
    //    AzureKinectSensor sensor1 = new AzureKinectSensor(p, configKinect1);

    //    /*** BODIES CONVERTERS ***/
    //    BodiesConverter bodiesConverter0 = new BodiesConverter(p, "kinectecConverter0");
    //    BodiesConverter bodiesConverter1 = new BodiesConverter(p, "kinectecConverter1");

    //    /*** BODIES DETECTION ***/
    //    // Basic configuration for the moment.
    //    BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
    //    BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

    //    /*** POSITION SELECTER ***/
    //    // Basic configuration for the moment.
    //    SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
    //    SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(p, bodiesSelectionConfiguration);

    //    /*** INSTANT GROUPS ***/
    //    // Basic configuration for the moment.
    //    InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
    //    InstantGroups frameGroups = new InstantGroups(p, instantGroupsConfiguration);

    //    /*** INTEGRATED GROUPS ***/
    //    // Basic configuration for the moment.
    //    IntegratedGroupsConfiguration integratedGroupsConfiguration = new IntegratedGroupsConfiguration();
    //    IntegratedGroups intgratedGroups = new IntegratedGroups(p, integratedGroupsConfiguration);

    //    /*** MORE TO COME ! ***/


    //    /*** LINKAGE ***/
    //    sensor0.Bodies.PipeTo(bodiesConverter0.InBodiesAzure);
    //    sensor1.Bodies.PipeTo(bodiesConverter1.InBodiesAzure);
    //    bodiesConverter0.OutBodies.PipeTo(bodiesDetection.InCamera1Bodies);
    //    bodiesConverter1.OutBodies.PipeTo(bodiesDetection.InCamera2Bodies);
    //    bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
    //    positionExtraction.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
    //    frameGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);

    //}

    //static void SingleKinectSkeletonsRecording(Pipeline p)
    //{
    //    /*** KINECT SENSOR ***/
    //    // Only need Skeleton for the moment.
    //    AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
    //    configKinect0.DeviceIndex = 0;
    //    configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
    //    AzureKinectSensor sensor0 = new AzureKinectSensor(p, configKinect0);

    //    /*** DATA STORING FOR PSI STUDIO ***/
    //    var store = PsiStore.Create(p, "SingleKinectSkeletonsRecording", "F:\\Stores");
    //    //store.Write(sensor0.ColorImage, "Color0");
    //    store.Write(sensor0.DepthDeviceCalibrationInfo, "DepthCalibration");
    //    store.Write(sensor0.Bodies, "Bodies");
    //}

    //static void GroupSinleIdentificationTesting(Pipeline p)
    //{
    //    var store = PsiStore.Open(p, "GroupsStoring", "F:\\Stores\\Free2");
    //    var Bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");

    //    /*** BODIES CONVERTERS ***/
    //    BodiesConverter bodiesConverter0 = new BodiesConverter(p, "converter0");

    //    /*** BODIES IDENTIFICATION ***/
    //    BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
    //    BodiesIdentification bodiesIdentification0 = new BodiesIdentification(p, bodiesIdentificationConfiguration);

    //    /*** BODIES DETECTION ***/
    //    // Basic configuration for the moment.
    //    BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();

    //    BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

    //    /*** Linkage ***/
    //    Bodies0.PipeTo(bodiesConverter0.InBodiesAzure);

    //    bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);

    //    bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera1Bodies);
    //    bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);

    //}

    //static void GroupsUsingRecords(Pipeline p)
    //{
    //    MathNet.Numerics.LinearAlgebra.Matrix<double>? calibration;
    //    if (!Helpers.Helpers.ReadCalibrationFromFile("calib.csv", out calibration))
    //        calibration = null;

    //    var store = PsiStore.Open(p, "GroupsStoring", "F:\\Stores\\Free2");
    //    var Bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");
    //    var Bodies1 = store.OpenStream<List<AzureKinectBody>>("Bodies1");

    //    /*** BODIES CONVERTERS ***/
    //    BodiesConverter bodiesConverter0 = new BodiesConverter(p, "converter0");
    //    BodiesConverter bodiesConverter1 = new BodiesConverter(p, "converter1");

    //    /*** BODIES IDENTIFICATION ***/
    //    BodiesIdentificationConfiguration bodiesIdentificationConfiguration = new BodiesIdentificationConfiguration();
    //    BodiesIdentification bodiesIdentification0 = new BodiesIdentification(p, bodiesIdentificationConfiguration);
    //    BodiesIdentification bodiesIdentification1 = new BodiesIdentification(p, bodiesIdentificationConfiguration);

    //    /*** BODIES DETECTION ***/
    //    // Basic configuration for the moment.
    //    BodiesSelectionConfiguration bodiesDetectionConfiguration = new BodiesSelectionConfiguration();
    //    bodiesDetectionConfiguration.Camera2ToCamera1Transformation = calibration;
    //    BodiesSelection bodiesDetection = new BodiesSelection(p, bodiesDetectionConfiguration);

    //    /*** POSITION SELECTER ***/
    //    // Basic configuration for the moment.
    //    SimpleBodiesPositionExtractionConfiguration bodiesSelectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
    //    SimpleBodiesPositionExtraction positionExtraction = new SimpleBodiesPositionExtraction(p, bodiesSelectionConfiguration);

    //    /*** INSTANT GROUPS ***/
    //    // Basic configuration for the moment.
    //    InstantGroupsConfiguration instantGroupsConfiguration = new InstantGroupsConfiguration();
    //    InstantGroups instantGroups = new InstantGroups(p, instantGroupsConfiguration);

    //    /*** MORE TO COME ! ***/


    //    /*** Linkage ***/
    //    Bodies0.PipeTo(bodiesConverter0.InBodiesAzure);
    //    Bodies1.PipeTo(bodiesConverter1.InBodiesAzure);

    //    bodiesConverter0.OutBodies.PipeTo(bodiesIdentification0.InCameraBodies);
    //    bodiesConverter1.OutBodies.PipeTo(bodiesIdentification1.InCameraBodies);

    //    bodiesIdentification0.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera1Bodies);
    //    bodiesIdentification1.OutBodiesIdentified.PipeTo(bodiesDetection.InCamera2Bodies);
    //    bodiesIdentification0.OutLearnedBodies.PipeTo(bodiesDetection.InCamera1LearnedBodies);
    //    bodiesIdentification1.OutLearnedBodies.PipeTo(bodiesDetection.InCamera2LearnedBodies);

    //    bodiesDetection.OutBodiesCalibrated.PipeTo(positionExtraction.InBodiesSimplified);
    //    positionExtraction.OutBodiesPositions.PipeTo(instantGroups.InBodiesPosition);
    //}

    static void HololensImporter(Pipeline p)
    {
        /*** REMOTE IMPORTER ! ***/
        //"10.44.193.26"
        RemoteImporter importer = new RemoteImporter(p, ReplayDescriptor.ReplayAll.Interval, "localhost");
        Console.WriteLine("connecting");
        if (!importer.Connected.WaitOne(-1))
        {
            throw new Exception("could not connect to server");
        }
        Console.WriteLine("connected");
        var store = PsiStore.Create(p, "UnityStreaming", "F:\\Stores");
        var frames = importer.Importer.OpenStream<EncodedImage>("frame");
        store.Write(frames, "Image");
        frames.Do(image => Console.WriteLine("Image recieved:" + image.Size.ToString()));
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
        //AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        //configKinect.DeviceIndex = 0;
        //configKinect.ColorResolution = Microsoft.Azure.Kinect.Sensor.ColorResolution.R720p;
        //configKinect.CameraFPS = Microsoft.Azure.Kinect.Sensor.FPS.FPS30;
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

        Console.WriteLine("Audio:");
        foreach (string name in Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices())
            Console.WriteLine(name);


        Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();

        Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(p, configuration);

        MediaCaptureConfiguration cfg = new Microsoft.Psi.Media.MediaCaptureConfiguration();

        MediaCapture capture = new Microsoft.Psi.Media.MediaCapture(p);

        var store = PsiStore.Create(p, "HTCStoring", "F:\\Stores");

        store.Write(audioCapture.Out, "Audio");

    }

    static void KinectPostures(Pipeline p)
    {
        //AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        //configKinect.DeviceIndex = 0;
        //configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        //AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

        //BodiesConverter bodiesConverter = new BodiesConverter(p);

        ////SimplePostuesConfiguration configuration = new SimplePostuesConfiguration();
        ////SimplePostures postures = new SimplePostures(p, configuration);

        //BodiesStatisticsConfiguration bsConfguration = new BodiesStatisticsConfiguration();
        //BodiesStatistics statistics = new BodiesStatistics(p, bsConfguration);

        //sensor.Bodies.PipeTo(bodiesConverter.InBodiesAzure);
        //bodiesConverter.OutBodies.PipeTo(statistics.InBodies);
    }

    static void testTobii(Pipeline p)
    {
        foreach(var sensor in TobiiSensor.AllDevices)
        {
            Console.WriteLine(sensor.DeviceName);
        }
        TobiiSensor tobii = new TobiiSensor(p);

    }

    static void TestConnectorAzureKinect(Pipeline p)
    {
        KinectAzureRemoteConnectorConfiguration config = new KinectAzureRemoteConnectorConfiguration();
        config.ActiveStreamNumber = 3;
        config.StartPort = 22822;
        KinectAzureRemoteConnector connector = new KinectAzureRemoteConnector(p, config);
        Console.WriteLine(connector.Name);

        var store = PsiStore.Create(p, "Remote", "F:\\Stores");

        store.Write(connector.OutColorImage, "Image");
        store.Write(connector.OutAudio, "Audio");
        store.Write(connector.OutBodies, "Bodies");


        //KinectAzureRemoteConnector connector2 = new KinectAzureRemoteConnector(p, config);
        //var store2 = PsiStore.Create(p, "Remote2", "F:\\Stores");

        //store2.Write(connector2.OutColorImage, "Image");
        //store2.Write(connector2.OutAudio, "Audio");
        //store2.Write(connector2.OutBodies, "Bodies");

    }

    static void testOpenFace(Pipeline p)
    {
        AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        configKinect.DeviceIndex = 0;
        configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);
        OpenFace face = new OpenFace(p);

        sensor.ColorImage.PipeTo(face.In);

    }


    static void WebRTC(Pipeline p)
    {
        WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
        config.WebsocketAddress = System.Net.IPAddress.Loopback;
        config.WebsocketPort = 80;
        config.AudioStreaming = false;
        config.PixelStreamingConnection = true;
        WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
        var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

        store.Write(stream.OutImage, "Image");
        //store.Write(stream.OutAudio, "Audio");
    }


    static void testUnity(Pipeline p)
    {
        //RemoteExporter exporter = new RemoteExporter(p, 11412, TransportKind.Tcp);
        //KeyboardReader.KeyboardReader keyboardReader = new KeyboardReader.KeyboardReader(p);
        //exporter.Exporter.Write<string>(keyboardReader.Out, "Test");

        //RemoteImporter posImp = new RemoteImporter(p, "localhost");
        //if (!posImp.Connected.WaitOne(-1))
        //{
        //    throw new Exception("could not connect to server");
        //}
        //var pos = posImp.Importer.OpenStream<TimeSpan>("Test");
        //pos.Do(vec => Console.WriteLine("posImp 1: " + vec.ToString()));

        RemoteImporter posImp2 = new RemoteImporter(p, "localhost");
        if (!posImp2.Connected.WaitOne(-1))
        {
            throw new Exception("could not connect to server");
        }
        var pos2 = posImp2.Importer.OpenStream<string>("Position");
        pos2.Do(vec => Console.WriteLine("posImp 2: " + vec));
        //var pos = posImp2.Importer.OpenStream<System.Numerics.Vector3>("Position2");
        //pos.Do(vec => Console.WriteLine("posImp : " + vec.ToString()));
    }

    static void testUnreal(Pipeline p)
    {
        HttpListenerConfiguration httpListenerConfiguration = new HttpListenerConfiguration();
        httpListenerConfiguration.Prefixes.Add("http://127.0.0.1:8888/psi/");
        HttpListener httpListener = new HttpListener(p, httpListenerConfiguration);

        UnrealRemoteConnectorConfiguration config = new UnrealRemoteConnectorConfiguration();
        config.Address = "http://127.0.0.1:30010/remote/object/call";
        
        UnrealRemoteConnector connector = new UnrealRemoteConnector(p, config);
        //UnrealActionRequest req = new UnrealActionRequest("BP_Vivian_2", "/Game/Levels/UEDPIE_0_MainLevel.MainLevel:PersistentLevel.", "Start Welcome");

        //connector.Send(req);

        var store = PsiStore.Create(p, "Unreal", "F:\\Stores");

        store.Write(connector.OutActionRequest, "Biopac");
    }

    static void testBipoac(Pipeline p)
    {
        Biopac.Biopac biopac = new Biopac.Biopac(p);
        var store = PsiStore.Create(p, "Biopac", "F:\\Stores");

        store.Write(biopac.Out, "Biopac");
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
        //HololensImporter(p);
        //GroupSinleIdentificationTesting(p);
        //GroupsUsingRecords(p);

        /*** HOLOLENS ***/
        //HololensImporter(p);

        //TestConnectorAzureKinect(p);

        //WebRTC(p);
        //testBipoac(p);
        //testUnity(p);
        //testUnreal(p);
        WebRTC(p);
        //TestOpenFace(p);
        //testTobii(p);
        // RunAsync the pipeline in non-blocking mode.
        p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
        // Wainting for an out key
        Console.WriteLine("Press any key to stop the application.");
        Console.ReadLine();
        // Stop correctly the pipeline.
        //p.Dispose();
    }
}
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.AzureKinect;
using NuitrackComponent;
using Groups.Instant;
using Groups.Integrated;
using Bodies;
using NatNetComponent;
using LabJackComponent;
using LabJack.LabJackUD;

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
        SimpleBodiesPositionExtractionConfiguration BodiesDetectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
        SimpleBodiesPositionExtraction BodiesDetection = new SimpleBodiesPositionExtraction(p, BodiesDetectionConfiguration);

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
        sensor.Bodies.PipeTo(BodiesDetection.InBodiesNuitrack);
        BodiesDetection.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
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
        BodiesDetectionConfiguration bodiesDetectionConfiguration = new BodiesDetectionConfiguration();
        BodiesDetection bodiesDetection = new BodiesDetection(p, bodiesDetectionConfiguration);

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

    static void GroupsRecording(Pipeline p)
    {
        /*** KINECT SENSOR ***/
        // Only need Skeleton for the moment.
        AzureKinectSensorConfiguration configKinect0 = new AzureKinectSensorConfiguration();
        configKinect0.DeviceIndex = 0;
        configKinect0.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor0 = new AzureKinectSensor(p, configKinect0);

        //AzureKinectSensorConfiguration configKinect1 = new AzureKinectSensorConfiguration();
        //configKinect1.DeviceIndex = 1;
        //configKinect1.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        //AzureKinectSensor sensor1 = new AzureKinectSensor(p, configKinect1);

        // Application
        RemoteImporter importer = new RemoteImporter(p, ReplayDescriptor.ReplayAll.Interval, "localhost");
        //Console.WriteLine("connecting");
        //if (!importer.Connected.WaitOne(-1))
        //{
        //    throw new Exception("could not connect to server");
        //}
        //Console.WriteLine("connected");
        //var group1 = importer.Importer.OpenStream<uint>("Group1");
        //var group2 = importer.Importer.OpenStream<uint>("Group2");
        //var group3 = importer.Importer.OpenStream<uint>("Group3");
        //var group4 = importer.Importer.OpenStream<uint>("Group4");
        //var group5 = importer.Importer.OpenStream<uint>("Group5");
        //group1.Do((e, s) => Console.WriteLine(e.ToString() + " "+ s.OriginatingTime));
        /*** DATA STORING FOR PSI STUDIO ***/
        var store = PsiStore.Create(p, "GroupsStoring", "F:\\Stores");
        store.Write(sensor0.ColorImage, "Color0");
        //store.Write(sensor0.DepthImage, "Depth0");
        store.Write(sensor0.Bodies, "Bodies0");
        //store.Write(sensor1.ColorImage, "Color1");
        //store.Write(sensor1.DepthImage, "Depth1");
        //store.Write(sensor1.Bodies, "Bodies1");
        //store.Write(group1, "Group1");
        //store.Write(group2, "Group2");
        //store.Write(group3, "Group3");
        //store.Write(group4, "Group4");
        //store.Write(group5, "Group5");
        //p.Run();
    }

    static void GroupsUsingRecords(Pipeline p)
    {
        var store = PsiStore.Open(p, "GroupsStoring", "F:\\Stores\\GroupsStoring.0000");
        var Bodies0 = store.OpenStream<List<AzureKinectBody>>("Bodies0");
        var Bodies1 = store.OpenStream<List<AzureKinectBody>>("Bodies1");

        /*** BODY DETECTOR ***/
        // Basic configuration for the moment.
        SimpleBodiesPositionExtractionConfiguration BodiesDetectionConfiguration = new SimpleBodiesPositionExtractionConfiguration();
        SimpleBodiesPositionExtraction BodiesDetection = new SimpleBodiesPositionExtraction(p, BodiesDetectionConfiguration);

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
        //Bodies1.Process().ToList().ForEach(body => Console.WriteLine(body.TrackingId));
        Bodies0.PipeTo(BodiesDetection.InBodiesAzure);
        BodiesDetection.OutBodiesPositions.PipeTo(frameGroups.InBodiesPosition);
        frameGroups.OutInstantGroups.PipeTo(intgratedGroups.InInstantGroups);
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
        commands.ResponseCommand.GetterType = ResponseCommand.GetType.First_Next;

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

        configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

        Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();
        Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(p, configuration);

        var store = PsiStore.Create(p, "KinectAudioStoring", "F:\\Stores");
        store.Write(sensor.ColorImage, "Image");
        store.Write(sensor.DepthDeviceCalibrationInfo, "DepthCalibration");
        store.Write(sensor.Bodies, "Bodies");
        store.Write(audioCapture.Out, "Audio");
    }

    static void Main(string[] args)
    {
        // Enabling diagnotstics !!!
        Pipeline p = Pipeline.Create(enableDiagnostics: true);

        //LabJackNatNetTesting(p);
        KinectVideoSoundTesting(p);

        /*** GROUPS TESTING ***/
        //GroupsTesting(p);
        // GroupsAzureTesting(p);

        /*** Record Groups ***/
        //GroupsRecording(p);
        //GroupsUsingRecords(p);

        /*** HOLOLENS ***/
        //HololensImporter(p);

        // RunAsync the pipeline in non-blocking mode.
        p.RunAsync();
        // Wainting for an out key
        Console.WriteLine("Press any key to stop the application.");
        Console.ReadLine();
        // Stop correctly the pipeline.
        p.Dispose();
    }
}
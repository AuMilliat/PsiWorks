using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Components;
using Microsoft.Psi.Remoting;
using NuitrackComponent;
using Groups.Instant;
using Groups.Integrated;

class Program
{
    static void GroupsTesting(Pipeline p)
    {
        /*** NUITRACK SENSOR ***/
        // Only need Skeleton for the moment.
        NuitrackCoreConfiguration configNuitrack = new NuitrackCoreConfiguration();
        configNuitrack.ActivationKey = "license:34821:ZvAVGW03StUh056F";
        //configNuitrack.OutputColor = false;
        //configNuitrack.OutputDepth = false;

        NuitrackSensor sensor = new NuitrackSensor(p, configNuitrack);

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
        sensor.Bodies.BridgeTo(frameGroups);
        frameGroups.OutInstantGroups.BridgeTo(intgratedGroups);

        /*** DATA STORING FOR PSI STUDIO ***/
        var store = PsiStore.Create(p, "GroupsStoring", Path.Combine(Directory.GetCurrentDirectory(), "Stores"));
        store.Write(sensor.ColorImage, "Image");
        store.Write(frameGroups.OutInstantGroups, "InstantGroups");
        store.Write(intgratedGroups.OutIntegratedGroups, "IntegratedGroups");
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
    static void Main(string[] args)
    {
        // Enabling diagnotstics !!!
        Pipeline p = Pipeline.Create(enableDiagnostics: true);

        /*** GROUPS TESTING ***/
        GroupsTesting(p);

        /*** HOLOLENS ***/
        //HololensImporter(p);

        // RunAsync the pipeline in non-blocking mode.
        p.RunAsync();
        // Wainting for an out key
        Console.WriteLine("Press any key to stop the recording.");
        Console.ReadLine();
        // Stop correctly the pipeline.
        p.Dispose();
    }
}
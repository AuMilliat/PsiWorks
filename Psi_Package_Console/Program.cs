using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Psi_Package_Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Pipeline p = Pipeline.Create(enableDiagnostics: true);

            //RemoteImporter posImp2 = new RemoteImporter(p, "10.44.192.249");
            //if (!posImp2.Connected.WaitOne(-1))
            //{
            //    throw new Exception("could not connect to server");
            //}
            //var pos = posImp2.Importer.OpenStream<string>("Topic");
            //pos.Do(vec => Console.WriteLine("posImp : " + vec));

            //RemoteExporter remoteExporter = new RemoteExporter(p, TransportKind.Tcp);
            //var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
            //var strinOut = p.CreateEmitter<string>(p, "POut");
            //remoteExporter.Exporter.Write(strinOut, "Toto");
            //timer.Out.Do((data, enveloppe) => { strinOut.Post($"Message @ {enveloppe.OriginatingTime} : {data}", enveloppe.OriginatingTime); });
            RemoteClockExporter exporter = new RemoteClockExporter(11511);
           
            // RunAsync the pipeline in non-blocking mode.
            p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            // Wainting for an out key
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            // Stop correctly the pipeline.
            p.Dispose();

        }
    }
}

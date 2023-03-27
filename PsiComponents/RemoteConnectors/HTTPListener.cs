using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Psi.Components;
using Microsoft.Psi;
using System.IO;

namespace RemoteConnectors
{
    public class HttpListenerConfiguration
    {
        public List<string> Prefixes = new List<string>();
    }

    public class HttpListener : ISourceComponent, IProducer<HttpListenerRequest>
    {
        private System.Net.HttpListener Listener;
        private Thread? captureThread = null;
        private bool shutdown = false;
        public Emitter<HttpListenerRequest> Out { get; private set; }

        public HttpListener(Pipeline parent, HttpListenerConfiguration configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Listener = new System.Net.HttpListener();
            foreach (string s in configuration.Prefixes)
            {
                Listener.Prefixes.Add(s);
            }
            Out = parent.CreateEmitter<HttpListenerRequest>(this, nameof(Out));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Listener.Start();
            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            throw new NotImplementedException();
        }

        private void CaptureThreadProc()
        {
            while (!this.shutdown)
            {
                HttpListenerContext context = Listener.GetContext();

                byte[] b = Encoding.UTF8.GetBytes("ACK");
                context.Response.StatusCode = 200;
                context.Response.KeepAlive = false;
                context.Response.ContentLength64 = b.Length;

                Out.Post(context.Request, DateTime.UtcNow);
                Console.WriteLine(context.ToString());  
            }
        }
    }
}

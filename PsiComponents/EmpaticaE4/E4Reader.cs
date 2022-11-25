using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading;
using Microsoft.Psi.Components;
using Microsoft.Psi;
using System.Collections.Generic;

namespace Test2
{

    internal sealed class E4Reader : ISourceComponent
    {
        public Emitter<float> BVP { get; private set; }
        public Emitter<float> GSR { get; private set; }
        public Emitter<float> TAG { get; private set; }
        private Thread captureThread = null;
        private bool shutdown = false;
        private List<double[]> bufferBVP = new List<double[]>();
        private List<double[]> bufferGSR = new List<double[]>();

        public E4Reader(Pipeline pipeline)
        {
            AsynchronousClient.StartClient();
            this.BVP = pipeline.CreateEmitter<float>(this, nameof(this.BVP));
            this.GSR = pipeline.CreateEmitter<float>(this, nameof(this.GSR));
            this.TAG = pipeline.CreateEmitter<float>(this, nameof(this.TAG));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
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
                string data = AsynchronousClient.Data();
                string[] processedData1 = data.Split(';');
                int c = processedData1.Length;
                if (c > 0)
                {
                    for (int i = 0; i < c; i++)
                    {
                        String[] processedData2 = processedData1[i].Split(' ');
                        if (processedData2.Length > 2)
                        {
                            float processedDataValue = float.Epsilon;
                            float.TryParse(processedData2[2], out processedDataValue);
                            if (processedDataValue != float.Epsilon)
                            {
                                double timeE4 = 0;
                                double.TryParse(processedData2[1], out timeE4);
                                if (timeE4 != 0)
                                {
                                    if (processedData2[0] == "E4_Bvp")
                                    {
                                        double[] test2 = new double[2] { timeE4, processedDataValue};
                                        this.bufferBVP.Add(test2);
                                    }
                                    else if (processedData2[0] == "E4_Gsr")
                                    {
                                        double[] test2 = new double[2] { timeE4, processedDataValue};
                                        this.bufferGSR.Add(test2);
                                    }
                                    else if (processedData2[0] == "E4_Tag")
                                    {
                                        DateTime te = UnixTimeStampToDateTime(timeE4);
                                        DateTime te2 = te.ToUniversalTime();
                                        TAG.Post(1, te2);
                                    }
                                }
                            }
                        }
                    }
                    this.bufferGSR = this.bufferGSR.OrderBy(x => x[0]).ToList();
                    this.bufferGSR = this.bufferGSR.GroupBy(x => x[0]).Select(x => x.First()).ToList();
                    this.bufferBVP = this.bufferBVP.OrderBy(x => x[0]).ToList();
                    this.bufferBVP = this.bufferBVP.GroupBy(x => x[0]).Select(x => x.First()).ToList();

                    int c1 = this.bufferGSR.Count;
                    if (c1 > 10)
                    {
                        for (int i = 0; i < c1-7; i++)
                        {
                            double[] test2 = this.bufferGSR[0];
                            DateTime te = UnixTimeStampToDateTime(test2[0]);
                            DateTime te2 = te.ToUniversalTime();
                            try
                            {
                                this.GSR.Post((float)test2[1], te2);
                                this.bufferGSR.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                        }
                    }
                    c1 = bufferBVP.Count;
                    if (c1 > 20)
                    {
                        for (int i = 0; i < c1 - 10; i++)
                        {
                            double[] test2 = this.bufferBVP[0];
                            DateTime te = UnixTimeStampToDateTime(test2[0]);
                            DateTime te2 = te.ToUniversalTime();
                            try
                            {
                                this.BVP.Post((float)test2[1], te2);
                                this.bufferBVP.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}

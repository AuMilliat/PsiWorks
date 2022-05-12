﻿using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Components;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MathNet.Numerics.Statistics;
using Helpers;

namespace Bodies
{
    public class BodiesStatisticsConfiguration
    {
        /// <summary>
        /// Gets or sets the confidence level used for statisticq.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;
  
        /// <summary>
        ///  File in csv format to write stats.
        /// </summary>
        public string StoringPath { get; set; } = "./Stats.csv";
    }
    public class BodiesStatistics : Subpipeline
    {
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;

        protected string StatsCount = "";

        protected Dictionary<uint, StatisticBody> Data = new Dictionary<uint, StatisticBody>();

        protected BodiesStatisticsConfiguration Configuration;
        public BodiesStatistics(Pipeline pipeline, BodiesStatisticsConfiguration? configuration) : base(pipeline)
        {
            Configuration = configuration ?? new BodiesStatisticsConfiguration(); 
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));
            InBodiesConnector.Out.Do(Process);
        }

        public override void Dispose()
        {
            StatsCount = "body_id;bone_id;count;mean;std_dev;var\n";
            foreach (var body in Data)
            {
                foreach (var bone in body.Value.BonesValues)
                {
                    var std = Statistics.MeanStandardDeviation(bone.Value);
                    //var five = Statistics.FiveNumberSummary(Data[body.Id].BonesValues[(bone.ParentJoint, bone.ChildJoint)]);
                    var variance = Statistics.MeanVariance(bone.Value);
                    string statis = body.Key.ToString() +";" + bone.Key.Item1.ToString() +"-"+ bone.Key.Item2.ToString() + ";" + bone.Value.Count.ToString() + ";" + std.Item1.ToString() + ";" + std.Item2.ToString() + ";" + variance.Item2.ToString();
                    StatsCount += statis + "\n";
                }
                StatsCount += "\n";
            }

            File.WriteAllText(Configuration.StoringPath, StatsCount);
            base.Dispose();
        }
        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            foreach (SimplifiedBody body in bodies)
            {
                if (!Data.ContainsKey(body.Id))
                    Data.Add(body.Id, new StatisticBody());
                foreach (var bone in AzureKinectBody.Bones)
                    if (body.Joints[bone.ParentJoint].Item1 >= Configuration.ConfidenceLevel && body.Joints[bone.ChildJoint].Item1 >= Configuration.ConfidenceLevel)
                        Data[body.Id].BonesValues[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
            }
        }
    }

    public class StatisticBody
    {
        public Dictionary<(JointId, JointId), List<double>> BonesValues = new Dictionary<(JointId, JointId), List<double>>();

        public StatisticBody()
        {
            foreach (var bone in AzureKinectBody.Bones)
                BonesValues.Add(bone, new List<double>());
        }
    }
}

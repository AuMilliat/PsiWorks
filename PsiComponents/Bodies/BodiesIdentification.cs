using MathNet.Spatial.Euclidean;
using MathNet.Numerics.Statistics;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;
using Helpers;

namespace Bodies
{
    public class BodiesIdentificationConfiguration
    {
        /// <summary>
        /// Gets or sets the bone list used.
        /// </summary>
        public List<(JointId ChildJoint, JointId ParentJoint)> BonesUsedForCorrespondence { get; set; } = new List<(JointId, JointId)>
        {
            (JointId.SpineNavel, JointId.Pelvis),
            (JointId.SpineChest, JointId.SpineNavel),
            (JointId.Neck, JointId.SpineChest),
            (JointId.ClavicleLeft, JointId.SpineChest),
            (JointId.ShoulderLeft, JointId.ClavicleLeft),
            (JointId.ElbowLeft, JointId.ShoulderLeft),
            (JointId.WristLeft, JointId.ElbowLeft),
            //(JointId.HandLeft, JointId.WristLeft),
            //(JointId.HandTipLeft, JointId.HandLeft),
            //(JointId.ThumbLeft, JointId.WristLeft),
            (JointId.ClavicleRight, JointId.SpineChest),
            (JointId.ShoulderRight, JointId.ClavicleRight),
            (JointId.ElbowRight, JointId.ShoulderRight),
            (JointId.WristRight, JointId.ElbowRight),
            //(JointId.HandRight, JointId.WristRight),
            //(JointId.HandTipRight, JointId.HandRight),
            //(JointId.ThumbRight, JointId.WristRight),
            (JointId.HipLeft, JointId.Pelvis),
            (JointId.KneeLeft, JointId.HipLeft),
            (JointId.AnkleLeft, JointId.KneeLeft),
            (JointId.FootLeft, JointId.AnkleLeft),
            (JointId.HipRight, JointId.Pelvis),
            (JointId.KneeRight, JointId.HipRight),
            (JointId.AnkleRight, JointId.KneeRight),
            (JointId.FootRight, JointId.AnkleRight),
            (JointId.Head, JointId.Neck),
            //(JointId.Nose, JointId.Head),
            //(JointId.EyeLeft, JointId.Head),
            //(JointId.EarLeft, JointId.Head),
            //(JointId.EyeRight, JointId.Head),
            //(JointId.EarRight, JointId.Head)
        };

        /// <summary>
        /// Gets or sets maximum acceptable duration for correpondance in millisecond
        /// </summary>
        public TimeSpan MaximumIdentificationTime { get; set; } = new TimeSpan(0,0,1);

        /// <summary>
        /// Gets or sets maximum acceptable deviation for correpondance in meter
        /// </summary>
        public double MaximumDeviationAllowed { get; set; } = 0.05;
    }
    public class BodiesIdentification : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesIdentified { get; private set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
       // public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCameraBodiesConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<SimplifiedBody>> InCameraBodies => InCameraBodiesConnector.In;

        private BodiesIdentificationConfiguration Configuration { get; }

        private Dictionary<uint, uint> CorrespondanceMap = new Dictionary<uint, uint>();
        private Dictionary<uint, LearnedBody> LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearningBody> LearningBodies = new Dictionary<uint, LearningBody>();

        public BodiesIdentification(Pipeline parent, BodiesIdentificationConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new BodiesIdentificationConfiguration();

            InCameraBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCameraBodiesConnector));
            OutBodiesIdentified = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesIdentified));

            InCameraBodiesConnector.Out.Do(Process);
        }
        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> identifiedBodies = new List<SimplifiedBody>();
            List<uint> foundBodies = new List<uint>();
            List<uint> idsBodies = new List<uint>();
            foreach (var body in bodies)
            {
                if (CorrespondanceMap.ContainsKey(body.Id))
                {
                    idsBodies.Add(CorrespondanceMap[body.Id]);
                    idsBodies.Add(body.Id);
                    body.Id = CorrespondanceMap[body.Id];
                    LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    continue;
                }
                else if (LearnedBodies.ContainsKey(body.Id))
                {
                    LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    idsBodies.Add(body.Id);
                }
            }
            foreach (var body in bodies)
                if (!foundBodies.Contains(body.Id))
                    ProcessLearningBody(body, envelope.OriginatingTime, idsBodies);
            OutBodiesIdentified.Post(identifiedBodies, envelope.OriginatingTime);
        }

        private bool ProcessLearningBody(SimplifiedBody body, DateTime timestamp, List<uint> idsBodies)
        {
            if(!LearningBodies.ContainsKey(body.Id))
                LearningBodies.Add(body.Id, new LearningBody(body.Id, timestamp, Configuration.BonesUsedForCorrespondence));

            if (LearningBodies[body.Id].StillLearning(timestamp, Configuration.MaximumIdentificationTime))
                foreach (var bone in Configuration.BonesUsedForCorrespondence)
                    LearningBodies[body.Id].LearningBones[bone].Add(MathNet.Numerics.Distance.SSD(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
            else
            {
                LearnedBody newLearnedBody = LearningBodies[body.Id].GeneratorLearnedBody(Configuration.MaximumDeviationAllowed);
                foreach(var learnedBody in LearnedBodies)
                {
                    if (idsBodies.Contains(learnedBody.Key))
                        continue;
                    if(newLearnedBody.IsSameAs(learnedBody.Value, Configuration.MaximumDeviationAllowed))
                    {
                        learnedBody.Value.LastSeen = timestamp;
                        CorrespondanceMap[body.Id] = learnedBody.Key;
                        LearningBodies.Remove(body.Id);
                        return false;
                    }
                }
                newLearnedBody.LastSeen = timestamp;
                LearnedBodies.Add(body.Id, newLearnedBody);
                LearningBodies.Remove(body.Id);
                return false;
            }
            return true;
        }
    }

    internal class LearnedBody
    {
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), double> LearnedBones { get; private set; }
        public uint Id { get; private set; }
        public DateTime LastSeen { get; set; }
        public LearnedBody(uint id, Dictionary<(JointId ChildJoint, JointId ParentJoint), double> bones)
        {
            Id = id;
            LearnedBones = bones;
        }
        public bool IsSameAs(LearnedBody b, double maxDeviation)
        {
            List<double> diff = new List<double>();
            foreach (var iterator in LearnedBones)
                if(iterator.Value > 0.0 && b.LearnedBones[iterator.Key] > 0.0)
                    diff.Add(Math.Abs(iterator.Value - b.LearnedBones[iterator.Key]));
            var statistics = Statistics.MeanStandardDeviation(diff);
            return statistics.Item2 < maxDeviation;
        }
    }
    internal class LearningBody
    {
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>> LearningBones { get; set; }
        public uint Id { get; private set; }
        public DateTime CreationTime { get; private set; }

        public LearningBody(uint id, DateTime time, List<(JointId ChildJoint, JointId ParentJoint)> bones)
        {
            Id = id;
            CreationTime = time;
            LearningBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>>();
            foreach (var bone in bones)
                LearningBones.Add(bone, new List<double>());
        }
        public bool StillLearning(DateTime time, TimeSpan duration)
        {
            return (time - CreationTime) < duration;
        }
        public LearnedBody GeneratorLearnedBody(double maxStdDev)
        {
            Dictionary<(JointId ChildJoint, JointId ParentJoint), double> learnedBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), double>();
            foreach(var iterator in LearningBones)
            {
                var statistics = Statistics.MeanStandardDeviation(iterator.Value);
                if (statistics.Item2 < maxStdDev)
                    learnedBones[iterator.Key] = statistics.Item1;
                else
                    learnedBones[iterator.Key] = -1;
            }
            return new LearnedBody(Id, learnedBones);
        }
    }
}

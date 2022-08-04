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
        public TimeSpan MaximumIdentificationTime { get; set; } = new TimeSpan(0, 0, 1);

        /// <summary>
        /// Gets or sets minimum time for trying the correspondance below that time we trust the Kinect identification algo
        /// </summary>
        public TimeSpan MinimumIdentificationTime { get; set; } = new TimeSpan(0, 1, 0);

        /// <summary>
        /// Gets or sets maximum acceptable duration for between old id pop again without identification in millisecond
        /// </summary>
        public TimeSpan MaximumLostTime { get; set; } = new TimeSpan(0, 5, 0);

        /// <summary>
        /// Gets or sets maximum acceptable deviation for correpondance in meter
        /// </summary>
        public double MaximumDeviationAllowed { get; set; } = 0.005;
    }
    public class BodiesIdentification : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesIdentified { get; private set; }

        /// <summary>
        /// Gets the emitter of new learned bodies.
        /// </summary>
        public Emitter<List<LearnedBody>> OutLearnedBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCameraBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCameraBodies => InCameraBodiesConnector.In;

        private BodiesIdentificationConfiguration Configuration { get; }

        private Dictionary<uint, uint> CorrespondanceMap = new Dictionary<uint, uint>();
        private Dictionary<uint, LearnedBody> LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearningBody> LearningBodies = new Dictionary<uint, LearningBody>();
        private List<LearnedBody> NewLearnedBodies = new List<LearnedBody>();


        public BodiesIdentification(Pipeline parent, BodiesIdentificationConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new BodiesIdentificationConfiguration();
            InCameraBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCameraBodiesConnector));
            OutBodiesIdentified = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesIdentified));
            OutLearnedBodies = parent.CreateEmitter<List<LearnedBody>>(this, nameof(OutLearnedBodies));
            OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, nameof(OutBodiesRemoved));
            InCameraBodiesConnector.Out.Do(Process);
        }

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> identifiedBodies = new List<SimplifiedBody>();
            List<uint> foundBodies = new List<uint>();
            List<uint> idsBodies = new List<uint>();
            List<uint> idsToRemove = new List<uint>();
            RemoveOldIds(envelope.OriginatingTime, ref idsToRemove);
            foreach (var body in bodies)
            {
                if (CorrespondanceMap.ContainsKey(body.Id))
                {
                    idsBodies.Add(CorrespondanceMap[body.Id]);
                    idsBodies.Add(body.Id);
                    body.Id = CorrespondanceMap[body.Id];
                    LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    LearnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    continue;
                }
                else if (LearnedBodies.ContainsKey(body.Id))
                {
                    if (envelope.OriginatingTime - LearnedBodies[body.Id].LastSeen < Configuration.MaximumLostTime || LearnedBodies[body.Id].SeemsTheSame(body, Configuration.MaximumDeviationAllowed))
                    {
                        LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                        LearnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                        identifiedBodies.Add(body);
                        foundBodies.Add(body.Id);
                        idsBodies.Add(body.Id);
                    }
                    else
                        idsToRemove.Add(body.Id);
                }
            }
            if (idsToRemove.Count > 0)
            { 
                RemoveIds(idsToRemove);
                OutBodiesRemoved.Post(idsToRemove, envelope.OriginatingTime);
            }
            foreach (var body in bodies)
                if (!foundBodies.Contains(body.Id))
                    ProcessLearningBody(body, envelope.OriginatingTime, idsBodies);
            if(NewLearnedBodies.Count > 0)
            {
                OutLearnedBodies.Post(NewLearnedBodies, envelope.OriginatingTime);
                NewLearnedBodies.Clear();
            }
            OutBodiesIdentified.Post(identifiedBodies, envelope.OriginatingTime);
        }

        private bool ProcessLearningBody(SimplifiedBody body, DateTime timestamp, List<uint> idsBodies)
        {
           
            if (!LearningBodies.ContainsKey(body.Id))
                LearningBodies.Add(body.Id, new LearningBody(body.Id, timestamp, Configuration.BonesUsedForCorrespondence));

            if (LearningBodies[body.Id].StillLearning(timestamp, Configuration.MaximumIdentificationTime))
                foreach (var bone in Configuration.BonesUsedForCorrespondence)
                    LearningBodies[body.Id].LearningBones[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
            else
            {
                List<LearnedBody> learnedBodiesNotVisible = new List<LearnedBody>();
                foreach (var learnedBody in LearnedBodies)
                {
                    if (idsBodies.Contains(learnedBody.Key))
                        continue;
                    if (timestamp - learnedBody.Value.LastSeen > Configuration.MinimumIdentificationTime)
                        learnedBodiesNotVisible.Add(learnedBody.Value);
                }
                LearnedBody newLearnedBody = LearningBodies[body.Id].GeneratorLearnedBody(Configuration.MaximumDeviationAllowed);
                NewLearnedBodies.Add(newLearnedBody);
                newLearnedBody.LastSeen = timestamp;
                newLearnedBody.LastPosition = body.Joints[JointId.Pelvis].Item2;
                LearningBodies.Remove(body.Id);
                uint correspondanceId = 0;
                if(learnedBodiesNotVisible.Count > 0)
                    correspondanceId = newLearnedBody.FindClosest(learnedBodiesNotVisible, Configuration.MaximumDeviationAllowed);
                if(correspondanceId > 0)
                    CorrespondanceMap[body.Id] = correspondanceId;
                else
                {
                    LearnedBodies.Add(body.Id, newLearnedBody);
                    idsBodies.Add(body.Id);
                }
                return false;
            }
            return true;
        }

        private void RemoveOldIds(DateTime current, ref List<uint> idsToRemove)
        {
            foreach (var body in LearnedBodies)
                if((current - body.Value.LastSeen) > Configuration.MaximumLostTime)
                    idsToRemove.Add(body.Key);

            RemoveIds(idsToRemove);
        }

        private void RemoveIds(List<uint> ids)
        {
            foreach (uint id in ids)
            {
                LearnedBodies.Remove(id);
                CorrespondanceMap.Remove(id);
                foreach (var iterator in CorrespondanceMap)
                {
                    if (iterator.Value == id)
                    {
                        CorrespondanceMap.Remove(iterator.Key);
                        break;
                    }
                }
            }
        }
    }
}

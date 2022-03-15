using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using nuitrack;

namespace Groups.Instant
{
    public class InstantGroupsConfiguration
    {
        /// <summary>
        /// Gets or sets the distance threshold between skeletons for constitute a grou^p.
        /// </summary>
        public float DistanceThreshold { get; set; } = 800;

        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Nuitrack.
        /// </summary>
        public JointType NuitrackJointAsPosition { get; set; } = JointType.Torso;

        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Nuitrack.
        /// </summary>
        public Microsoft.Azure.Kinect.BodyTracking.JointId AzureJointAsPosition { get; set; } = Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis;
    }
    public class InstantGroups : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutInstantGroups { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<Skeleton>> BodiesNuitrackInConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<Skeleton>> NuitrackBodiesIn => BodiesNuitrackInConnector.In;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<AzureKinectBody>> BodiesAzureInConnector;

        // Receiver that encapsulates the input list of Azure skeletons
        public Receiver<List<AzureKinectBody>> BodiesAzureIn => BodiesAzureInConnector.In;

        private InstantGroupsConfiguration Configuration { get; }
        public InstantGroups(Pipeline parent, InstantGroupsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) 
            : base(parent, name, defaultDeliveryPolicy)
        {
            if(configuration == null)
                Configuration = new InstantGroupsConfiguration();
            else
                Configuration = configuration;
            BodiesNuitrackInConnector = CreateInputConnectorFrom<List<Skeleton>>(parent, nameof(BodiesNuitrackInConnector));
            BodiesAzureInConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(parent, nameof(BodiesAzureInConnector));
            OutInstantGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutInstantGroups));
            BodiesAzureInConnector.Out.Do(Process);
            BodiesNuitrackInConnector.Out.Do(Process);
        }

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            List<KeyValuePair<uint, System.Numerics.Vector3>> skeletons = new List<KeyValuePair<uint, System.Numerics.Vector3>>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add(new KeyValuePair<uint, System.Numerics.Vector3>((uint)skeleton.ID, Helpers.NuitrackToSystem(skeleton.GetJoint(Configuration.NuitrackJointAsPosition).Real)));
            }
            Process(skeletons, envelope);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            List<KeyValuePair<uint, System.Numerics.Vector3>> skeletons = new List<KeyValuePair<uint, System.Numerics.Vector3>>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add(new KeyValuePair<uint, System.Numerics.Vector3>(skeleton.TrackingId, Helpers.AzureToSystem(skeleton.Joints[Configuration.AzureJointAsPosition].Pose.Origin)));
            }
            Process(skeletons, envelope);
        }

        private void Process(List<KeyValuePair<uint, System.Numerics.Vector3>> skeletons, Envelope envelope)
        {
            Dictionary<uint, List<uint>> rawGroups = new Dictionary<uint, List<uint>>();
            for (int iterator1 = 0; iterator1 < skeletons.Count; iterator1++)
            {
                for (int iterator2 = iterator1+1; iterator2 < skeletons.Count; iterator2++)
                {
                    float distance = System.Numerics.Vector3.Distance(skeletons.ElementAt(iterator1).Value, skeletons.ElementAt(iterator2).Value);
                    if (distance > Configuration.DistanceThreshold)
                        continue;

                    if (rawGroups.ContainsKey(skeletons.ElementAt(iterator1).Key) && rawGroups.ContainsKey(skeletons.ElementAt(iterator2).Key))
                    {
                        rawGroups[skeletons.ElementAt(iterator1).Key].AddRange(rawGroups[skeletons.ElementAt(iterator2).Key]);
                        rawGroups.Remove(skeletons.ElementAt(iterator2).Key);
                    }
                    else if (rawGroups.ContainsKey(skeletons.ElementAt(iterator1).Key))
                    {
                        rawGroups[skeletons.ElementAt(iterator1).Key].Add(skeletons.ElementAt(iterator2).Key);
                    }
                    else if (rawGroups.ContainsKey(skeletons.ElementAt(iterator2).Key))
                    {
                        rawGroups[skeletons.ElementAt(iterator2).Key].Add(skeletons.ElementAt(iterator1).Key);
                    }
                    else 
                    {
                        List<uint> group = new List<uint>();
                        group.Add(skeletons.ElementAt(iterator1).Key);
                        group.Add(skeletons.ElementAt(iterator2).Key);
                        rawGroups.Add(skeletons.ElementAt(iterator1).Key, group); 
                    }
                }
            }

            Dictionary<uint, List<uint>> outData = new Dictionary<uint, List<uint>>();
            foreach (var rawGroup in rawGroups)
            {
                rawGroup.Value.Sort();
                List<uint> group = rawGroup.Value;
                uint uid = Helpers.CantorParingSequence(ref group);
                outData.Add(uid, group);
            }
            OutInstantGroups.Post(outData, envelope.OriginatingTime);
        }
    }
}

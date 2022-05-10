using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace Groups.Instant
{
    public class InstantGroupsConfiguration
    {
        /// <summary>
        /// Gets or sets the distance threshold between skeletons for constitute a grou^p.
        /// </summary>
        public double DistanceThreshold { get; set; } = 800.0;
    }

    public class InstantGroups : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutInstantGroups { get; private set; }

        /// <summary>
        /// Gets the  connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<Dictionary<uint, Vector3D>> InBodiesPositionConnector;
        
        /// <summary>
        /// Receiver that encapsulates the input list of skeletons
        /// </summary>
        public Receiver<Dictionary<uint, Vector3D>> InBodiesPosition => InBodiesPositionConnector.In;

        
        private InstantGroupsConfiguration Configuration { get; }
        public InstantGroups(Pipeline parent, InstantGroupsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) 
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new InstantGroupsConfiguration();
            InBodiesPositionConnector = CreateInputConnectorFrom<Dictionary<uint, Vector3D>>(parent, nameof(InBodiesPositionConnector));
            OutInstantGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutInstantGroups));
            InBodiesPositionConnector.Out.Do(Process);
        }

        private void Process(Dictionary<uint, Vector3D> skeletons, Envelope envelope)
        {
            Dictionary<uint, List<uint>> rawGroups = new Dictionary<uint, List<uint>>();
            for (int iterator1 = 0; iterator1 < skeletons.Count; iterator1++)
            {
                for (int iterator2 = iterator1+1; iterator2 < skeletons.Count; iterator2++)
                {
                    double distance = MathNet.Numerics.Distance.Euclidean(skeletons.ElementAt(iterator1).Value.ToVector(), skeletons.ElementAt(iterator2).Value.ToVector());
                    if (distance > Configuration.DistanceThreshold)
                        continue;

                    if (rawGroups.ContainsKey(skeletons.ElementAt(iterator1).Key) && rawGroups.ContainsKey(skeletons.ElementAt(iterator2).Key))
                    {
                        rawGroups[skeletons.ElementAt(iterator1).Key].AddRange(rawGroups[skeletons.ElementAt(iterator2).Key]);
                        rawGroups.Remove(skeletons.ElementAt(iterator2).Key);
                    }
                    else if (rawGroups.ContainsKey(skeletons.ElementAt(iterator1).Key))
                        rawGroups[skeletons.ElementAt(iterator1).Key].Add(skeletons.ElementAt(iterator2).Key);
                    else if (rawGroups.ContainsKey(skeletons.ElementAt(iterator2).Key))
                        rawGroups[skeletons.ElementAt(iterator2).Key].Add(skeletons.ElementAt(iterator1).Key);
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
                uint uid = Helpers.Helpers.CantorParingSequence(group);
                outData.Add(uid, group);
            }
            OutInstantGroups.Post(outData, envelope.OriginatingTime);
        }
    }
}

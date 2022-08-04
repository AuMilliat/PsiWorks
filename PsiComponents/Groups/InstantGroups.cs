using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace Groups
{
    public class InstantGroupsConfiguration
    {
        /// <summary>
        /// Gets or sets the distance threshold between skeletons for constitute a group.
        /// </summary>
        public double DistanceThreshold { get; set; } = 0.8;
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
                for (int iterator2 = iterator1 + 1; iterator2 < skeletons.Count; iterator2++)
                {
                    double distance = MathNet.Numerics.Distance.Euclidean(skeletons.ElementAt(iterator1).Value.ToVector(), skeletons.ElementAt(iterator2).Value.ToVector());
                    if (distance > Configuration.DistanceThreshold)
                        continue;

                    uint idBody1 = skeletons.ElementAt(iterator1).Key;
                    uint idBody2 = skeletons.ElementAt(iterator2).Key; 
                    if (rawGroups.ContainsKey(idBody1) && rawGroups.ContainsKey(idBody2))
                    {
                        rawGroups[idBody1].AddRange(rawGroups[idBody2]);
                        rawGroups.Remove(idBody2);
                    }
                    else if (rawGroups.ContainsKey(idBody1))
                        rawGroups[idBody1].Add(idBody2);
                    else if (rawGroups.ContainsKey(idBody2))
                        rawGroups[idBody2].Add(idBody1);
                    else 
                    {
                        List<uint> group = new List<uint>();
                        group.Add(idBody1);
                        group.Add(idBody2);
                        rawGroups.Add(idBody1, group); 
                    }
                }
            }

            ReduceGroups(ref rawGroups);

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

        protected void ReduceGroups(ref Dictionary<uint, List<uint>> groups)
        {
            bool call = false;
            foreach (var group in groups)
            {
                foreach (var id in group.Value)
                {
                    if (groups.ContainsKey(id) && group.Key != id)
                    {
                        groups[group.Key].AddRange(groups[id]);
                        groups.Remove(id);
                        call = true;
                        break;
                    }
                }
                if (call)
                    break;
            }
            if (call)
                ReduceGroups(ref groups);
        }
    }
}

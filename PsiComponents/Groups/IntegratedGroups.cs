using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Groups.Integrated
{
    public class IntegratedGroupsConfiguration
    {
        /// <summary>
        /// Gets or sets the weight for body on group.
        /// </summary>
        public double IncreaseWeightFactor { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the value of decreasong weight when a body is not found un a group.
        /// </summary>
        public double DecreaseWeightFactor { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the threshold value for removing a body not seen for a period of time (in second).
        /// </summary>
        public uint RemovingNotSeenBody { get; set; } = 90;
    }
    public class IntegratedGroups : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutIntegratedGroups { get; private set; }

        /// <summary>
        /// Gets list of instant groups.
        /// </summary>
        private Connector<Dictionary<uint, List<uint>>> InInstantGroupsConnector;
        /// <summary>
        // Receiver that encapsulates the instant groups
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> InInstantGroups => InInstantGroupsConnector.In;

        /// <summary>
        ///Gets the nuitrack connector of lists of removed skeletons 
        /// </summary>
        private Connector<List<uint>> InRemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<List<uint>> InRemovedBodies => InRemovedBodiesConnector.In;

        private IntegratedGroupsConfiguration Configuration { get; }
        public IntegratedGroups(Pipeline parent, IntegratedGroupsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new IntegratedGroupsConfiguration();
            InInstantGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InInstantGroupsConnector));
            InRemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InRemovedBodiesConnector));
            OutIntegratedGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutIntegratedGroups));
            InInstantGroupsConnector.Out.Do(Process);
            InRemovedBodiesConnector.Do(ProcessBodiesRemoving);
        }

        private Dictionary<uint ,DateTime> BodyDateTime = new Dictionary<uint, DateTime>();
        private Dictionary<uint, Dictionary<uint, double>> BodyToWeightedGroups = new Dictionary<uint, Dictionary<uint, double>>();

        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Integrationg data
            //TO DO describ basic algo
            foreach(var group in instantGroups)  
            {
                foreach (var body in group.Value)
                {
                    if (BodyToWeightedGroups.ContainsKey(body))
                    {
                        TimeSpan span = envelope.OriginatingTime - BodyDateTime[body];
                        if (BodyToWeightedGroups[body].ContainsKey(group.Key))
                            BodyToWeightedGroups[body][group.Key] += span.TotalMilliseconds * Configuration.IncreaseWeightFactor;
                        else 
                            BodyToWeightedGroups[body].Add(group.Key, Configuration.IncreaseWeightFactor);
                        foreach(var iterator in BodyToWeightedGroups[body])
                        {
                            if (iterator.Key == group.Key)
                                continue;
                            BodyToWeightedGroups[body][iterator.Key] -= span.TotalMilliseconds * Configuration.DecreaseWeightFactor;
                        }
                        BodyDateTime[body] = envelope.OriginatingTime;
                    }
                    else
                    {
                        Dictionary<uint, double> nDic = new Dictionary<uint, double>();
                        nDic.Add(group.Key, Configuration.IncreaseWeightFactor);
                        BodyToWeightedGroups.Add(body, nDic);
                        BodyDateTime.Add(body, envelope.OriginatingTime);
                    }
                }
            }

            // Cleaning old bodies
            List<uint> toRemove = new List<uint>();
            foreach(var body in BodyDateTime)
            {
                TimeSpan span = envelope.OriginatingTime - body.Value;
                if(span.Seconds > Configuration.RemovingNotSeenBody)
                    toRemove.Add(body.Key);
            }
            foreach (uint bodyToRemove in toRemove)
            {
                BodyToWeightedGroups.Remove(bodyToRemove);
                BodyDateTime.Remove(bodyToRemove);
            }

            // Generating Interated Groups
            Dictionary<uint, List<uint>> integratedGroups = new Dictionary<uint, List<uint>>();
            foreach(var iterator in BodyToWeightedGroups)
            {
                var list = iterator.Value.ToList();
                list.Sort((x, y) => x.Value.CompareTo(y.Value));
                uint groupId = list.ElementAt(0).Key;
                if (integratedGroups.ContainsKey(groupId))
                    integratedGroups[iterator.Key].Add(groupId);
                else
                {
                    List<uint> groupList = new List<uint>();
                    groupList.Add(iterator.Key);
                    integratedGroups.Add(groupId, groupList);
                }
            }
            OutIntegratedGroups.Post(integratedGroups, envelope.OriginatingTime);
        }

        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock (this)
            {
                foreach (uint id in idsToRemove)
                {
                    BodyDateTime.Remove(id);
                    uint groupId = 0;
                    foreach (var group in BodyToWeightedGroups)
                    {
                        if (group.Value.ContainsKey(id))
                        {
                            groupId = group.Key;
                            break;
                        }
                    }
                    BodyToWeightedGroups[groupId].Remove(id);
                    if (BodyToWeightedGroups[groupId].Count < 1)
                        BodyToWeightedGroups.Remove(groupId);
                }
            }
        }
    }
}

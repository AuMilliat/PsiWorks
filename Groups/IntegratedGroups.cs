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

        // Receiver that encapsulates the instant groups
        public Receiver<Dictionary<uint, List<uint>>> InInstantGroups => InInstantGroupsConnector.In;

        private IntegratedGroupsConfiguration Configuration { get; }
        public IntegratedGroups(Pipeline parent, IntegratedGroupsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            if (configuration == null)
                Configuration = new IntegratedGroupsConfiguration();
            else
                Configuration = configuration;
            InInstantGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InInstantGroupsConnector));
            OutIntegratedGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutIntegratedGroups));
            InInstantGroupsConnector.Out.Do(Process);
        }

        private Dictionary<uint ,DateTime> bodyDateTime = new Dictionary<uint, DateTime>();
        private Dictionary<uint, Dictionary<uint, double>> bodyToWeightedGroups = new Dictionary<uint, Dictionary<uint, double>>();

        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Integrationg data
            //TO DO describ basic algo
            foreach(var group in instantGroups)  
            {
                foreach (var body in group.Value)
                {
                    if (bodyToWeightedGroups.ContainsKey(body))
                    {
                        TimeSpan span = envelope.OriginatingTime - bodyDateTime[body];
                        if (bodyToWeightedGroups[body].ContainsKey(group.Key))
                        {
                            bodyToWeightedGroups[body][group.Key] += span.TotalMilliseconds * Configuration.IncreaseWeightFactor;
                        }
                        else 
                        {
                            bodyToWeightedGroups[body].Add(group.Key, Configuration.IncreaseWeightFactor);
                        }
                        foreach(var iterator in bodyToWeightedGroups[body])
                        {
                            if (iterator.Key == group.Key)
                                continue;
                            bodyToWeightedGroups[body][iterator.Key] -= span.TotalMilliseconds * Configuration.DecreaseWeightFactor;
                        }
                        bodyDateTime[body] = envelope.OriginatingTime;
                    }
                    else
                    {
                        Dictionary<uint, double> nDic = new Dictionary<uint, double>();
                        nDic.Add(group.Key, Configuration.IncreaseWeightFactor);
                        bodyToWeightedGroups.Add(body, nDic);
                        bodyDateTime.Add(body, envelope.OriginatingTime);
                    }
                }
            }

            // Cleaning old bodies
            List<uint> toRemove = new List<uint>();
            foreach(var body in bodyDateTime)
            {
                TimeSpan span = envelope.OriginatingTime - body.Value;
                if(span.Seconds > Configuration.RemovingNotSeenBody)
                    toRemove.Add(body.Key);
            }
            foreach (uint bodyToRemove in toRemove)
            {
                bodyToWeightedGroups.Remove(bodyToRemove);
                bodyDateTime.Remove(bodyToRemove);
            }

            // Generating Interated Groups
            Dictionary<uint, List<uint>> integratedGroups = new Dictionary<uint, List<uint>>();
            foreach(var iterator in bodyToWeightedGroups)
            {
                var list = iterator.Value.ToList();
                list.Sort((x, y) => x.Value.CompareTo(y.Value));
                uint groupId = list.ElementAt(0).Key;
                if (integratedGroups.ContainsKey(groupId))
                {
                    integratedGroups[iterator.Key].Add(groupId);
                }
                else
                {
                    List<uint> groupList = new List<uint>();
                    groupList.Add(iterator.Key);
                    integratedGroups.Add(groupId, groupList);
                }
            }
            OutIntegratedGroups.Post(integratedGroups, envelope.OriginatingTime);
        }
    }
}

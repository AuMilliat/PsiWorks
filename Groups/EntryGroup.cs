using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Groups.Entry
{
    public class EntryGroupsConfiguration
    {
        /// <summary>
        /// Gets or sets the threshold time (in second).
        /// </summary>
        public uint GroupFormationDelay { get; set; } = 10;
    }
    public class EntryGroups : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutFormedEntryGroups { get; private set; }

        /// <summary>
        /// Gets the emitter of groups forming.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutFormingEntryGroups { get; private set; }

        /// <summary>
        /// Gets list of instant groups.
        /// </summary>
        private Connector<Dictionary<uint, List<uint>>> InstantGroupsInConnector;

        // Receiver that encapsulates the instant groups
        public Receiver<Dictionary<uint, List<uint>>> InstantGroupsIn => InstantGroupsInConnector.In;

        private EntryGroupsConfiguration Configuration { get; }
        public EntryGroups(Pipeline parent, EntryGroupsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            if (configuration == null)
                Configuration = new EntryGroupsConfiguration();
            else
                Configuration = configuration;
            InstantGroupsInConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InstantGroupsInConnector));
            OutFormedEntryGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutFormedEntryGroups));
            OutFormingEntryGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutFormingEntryGroups));
            InstantGroupsInConnector.Out.Do(Process);
        }

        private Dictionary<uint, DateTime> groupDateTime = new Dictionary<uint, DateTime>();
        private List<uint> formedEntryGroups = new List<uint>();
        //private Dictionary<uint, List<uint>> bodyToWeightedGroups = new Dictionary<uint, List<uint>>();

        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Integrationg data
            //TO DO describ basic algo
            foreach (var group in instantGroups)
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
                        foreach (var iterator in bodyToWeightedGroups[body])
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
            foreach (var body in bodyDateTime)
            {
                TimeSpan span = envelope.OriginatingTime - body.Value;
                if (span.Seconds > Configuration.RemovingNotSeenBody)
                    toRemove.Add(body.Key);
            }
            foreach (uint bodyToRemove in toRemove)
            {
                bodyToWeightedGroups.Remove(bodyToRemove);
                bodyDateTime.Remove(bodyToRemove);
            }

            // Generating Interated Groups
            Dictionary<uint, List<uint>> integratedGroups = new Dictionary<uint, List<uint>>();
            foreach (var iterator in bodyToWeightedGroups)
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

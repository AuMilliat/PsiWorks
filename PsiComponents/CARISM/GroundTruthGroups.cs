using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace Groups.GroundTruthGroups
{
    public class GroundTruthGroups : Subpipeline
    {
        /// <summary>
        /// Gets the connector of lists of groups.
        /// </summary>
        private Connector<Dictionary<uint, List<uint>>> InGroupsConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of groups
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> InGroups => InGroupsConnector.In;

        /// <summary>
        /// Gets the connector of lists of groups.
        /// </summary>
        private Connector<KeyValuePair<uint, uint>> InGroundTruthConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of groups
        /// </summary>
        public Receiver<KeyValuePair<uint, uint>> InGroundTruth => InGroundTruthConnector.In;

        private Dictionary<uint, List<uint>> lastGroups;

        public GroundTruthGroups(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            lastGroups = new Dictionary<uint, List<uint>>();

            InGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InGroupsConnector));
            InGroundTruthConnector = CreateInputConnectorFrom<KeyValuePair<uint, uint>>(parent, nameof(InGroundTruthConnector));

            InGroupsConnector.Out.Do(Process);
            InGroundTruthConnector.Do(ProcessTruth);
        }

        private void Process(Dictionary<uint, List<uint>> groups, Envelope envelope)
        {
            if (CheckIfSame(groups))
                return;
            lastGroups = groups;
        }

        private void ProcessTruth(KeyValuePair<uint, uint> thruth, Envelope envelope)
        {

        }

        private bool CheckIfSame(Dictionary<uint, List<uint>> groups)
        { 
            foreach(var group in groups)
            {
                if (lastGroups.ContainsKey(group.Key))
                    foreach(uint id in group.Value)
                        if (!lastGroups[group.Key].Contains(id))
                            return false;
                else
                    return false;
            }
            return true;
        }
    }
}

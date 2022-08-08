using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace GroundTruthGroups
{
    public class TruthCentralizer : Subpipeline, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    
        protected readonly string GroupBase = "Group";

        protected string groupDesc = "";
        public string GroupDesc
        {
            get => groupDesc;
            set => SetProperty(ref groupDesc, value);
        }

        private Connector<uint> InGroup1Connector;
        public Receiver<uint> InGroup1 => InGroup1Connector.In;

        private Connector<uint> InGroup2Connector;
        public Receiver<uint> InGroup2 => InGroup2Connector.In;

        private Connector<uint> InGroup3Connector;
        public Receiver<uint> InGroup3 => InGroup3Connector.In;

        private Connector<uint> InGroup4Connector;
        public Receiver<uint> InGroup4 => InGroup4Connector.In;

        private Connector<uint> InGroup5Connector;
        public Receiver<uint> InGroup5 => InGroup5Connector.In;

        public Emitter<(uint, uint)> OutTruth { get; private set; }

        private Dictionary<uint, uint> Truth = new Dictionary<uint, uint>();

        public TruthCentralizer(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
         : base(parent, name, defaultDeliveryPolicy)
        {
            InGroup1Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup1Connector));
            InGroup2Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup2Connector));
            InGroup3Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup3Connector));
            InGroup4Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup4Connector));
            InGroup5Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup5Connector));
            OutTruth = parent.CreateEmitter<(uint, uint)>(this, nameof(OutTruth));

            InGroup1Connector.Do(Process1);
            InGroup2Connector.Do(Process2);
            InGroup3Connector.Do(Process3);
            InGroup4Connector.Do(Process4);
            InGroup5Connector.Do(Process5);
        }

        protected void Process((uint, uint) data, Envelope envelope)
        {
            Truth[data.Item1] = data.Item2;
            OutTruth.Post(data, envelope.OriginatingTime);
            GroupDesc = "";
            foreach (var iterator in Truth)
                GroupDesc += iterator.Key.ToString() + "-" + iterator.Value.ToString() + "|";
            
        }
        protected void Process1(uint data, Envelope envelope)
        {
            Process((1, data), envelope);
        }
        protected void Process2(uint data, Envelope envelope)
        {
            Process((2, data), envelope);
        }
        protected void Process3(uint data, Envelope envelope)
        {
            Process((3, data), envelope);
        }
        protected void Process4(uint data, Envelope envelope)
        {
            Process((4, data), envelope);
        }
        protected void Process5(uint data, Envelope envelope)
        {
            Process((5, data), envelope);
        }
    }

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
        private Connector<(uint, uint)> InGroundTruthConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of groups
        /// </summary>
        public Receiver<(uint, uint)> InGroundTruth => InGroundTruthConnector.In;

        private Dictionary<uint, List<uint>> lastGroups;

        public GroundTruthGroups(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            lastGroups = new Dictionary<uint, List<uint>>();

            InGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InGroupsConnector));
            InGroundTruthConnector = CreateInputConnectorFrom<(uint, uint)>(parent, nameof(InGroundTruthConnector));

            InGroupsConnector.Out.Do(Process);
            InGroundTruthConnector.Do(ProcessTruth);
        }

        private void Process(Dictionary<uint, List<uint>> groups, Envelope envelope)
        {
            if (CheckIfSame(groups))
                return;
            lastGroups = groups;
        }

        private void ProcessTruth((uint, uint) thruth, Envelope envelope)
        {
            Console.WriteLine(thruth.ToString());
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

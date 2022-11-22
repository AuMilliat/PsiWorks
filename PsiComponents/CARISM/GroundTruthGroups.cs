using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.IO;

namespace GroundTruthGroups
{
    internal class GroupInfo
    {
        public uint Count { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public GroupInfo(uint count, DateTime start)
        {
            Count = count;
            Start = start;
        }
    }

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

        private Connector<Dictionary<uint, List<uint>>> InDetectedGroupConnector;
        public Receiver<Dictionary<uint, List<uint>>> InDetectedGroup => InDetectedGroupConnector.In;

        public Emitter<(uint, uint)> OutTruth { get; private set; }

        private Dictionary<uint, GroupInfo> TruthProcess = new Dictionary<uint, GroupInfo>();

        private Dictionary<uint, List<uint>> DetectedGroupProcess = new Dictionary<uint, List<uint>>();

        private List<GroupInfo> TruthGroups, DectetedGroups;

        protected ulong ResultsOk = 0, ResultsKo = 0; 

        public TruthCentralizer(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
         : base(parent, name, defaultDeliveryPolicy)
        {
            InGroup1Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup1Connector));
            InGroup2Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup2Connector));
            InGroup3Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup3Connector));
            InGroup4Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup4Connector));
            InGroup5Connector = CreateInputConnectorFrom<uint>(parent, nameof(InGroup5Connector));
            InDetectedGroupConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InDetectedGroupConnector));
            OutTruth = parent.CreateEmitter<(uint, uint)>(this, nameof(OutTruth));

            InGroup1Connector.Do(Process1);
            InGroup2Connector.Do(Process2);
            InGroup3Connector.Do(Process3);
            InGroup4Connector.Do(Process4);
            InGroup5Connector.Do(Process5);
            InDetectedGroupConnector.Do(ProcessDetected);

            TruthGroups = new List<GroupInfo>();
            DectetedGroups = new List<GroupInfo>();
        }

        public override void Dispose()
        {
            string StatsCount = "Ok;Ko;\n";
            StatsCount += ResultsOk.ToString() + ";" + ResultsKo.ToString() + ";\n\n";
            StatsCount += (ResultsOk / (ResultsKo + ResultsOk)).ToString();

            string groupDesc = "Count;Start;End;\n";
            foreach (var iterator in TruthProcess)
            {
                groupDesc += iterator.Key.ToString() + ";" + iterator.Value.Count.ToString() + ";"
                            + ";" + iterator.Value.Start.ToString() + ";"
                            + ";" + iterator.Value.End.ToString() + ";\n";
            }

            File.WriteAllText("TruthCentralizer.csv", StatsCount);
            base.Dispose();
        }

        protected void Process((uint, uint) data, Envelope envelope)
        {
            lock (this)
            {
                if(TruthProcess.ContainsKey(data.Item1))
                {
                    GroupInfo group = TruthProcess[data.Item1];
                    group.End = envelope.OriginatingTime;
                    TruthGroups.Add(group);
                    TruthProcess.Remove(data.Item1);
                }
                if (data.Item2 > 0)
                {
                    TruthProcess.Add(data.Item1, new GroupInfo(data.Item2, envelope.OriginatingTime));
                }
                
                GroupDesc = "";
                foreach (var iterator in TruthProcess)
                    GroupDesc += iterator.Key.ToString() + "-" + iterator.Value.Count.ToString() + "|";
            }
            
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

        protected void ProcessDetected(Dictionary<uint, List<uint>> data, Envelope envelope)
        {
            if(DetectedGroupProcess.Count == 0) 
            {
                return;
            }
            List<uint> list = new List<uint>();
            foreach(var iterator in data)
            { 
                list.Add((uint)iterator.Value.Count);
            }
            lock(this)
            {
                if (DetectedGroupProcess.Count != list.Count)
                {
                    ResultsKo++;
                    return;
                }
                var detectedCopy = DetectedGroupProcess.DeepClone();
                foreach (var iterator in DetectedGroupProcess)
                {
                    if(list.Contains((uint)iterator.Value.Count))
                    {
                        list.Remove((uint)iterator.Value.Count);
                        detectedCopy.Remove(iterator.Key);
                    }
                }
                if(list.Count != 0 || detectedCopy.Count != 0)
                {
                    ResultsKo++;
                    return;
                }
                ResultsOk++;
                return;
            }
        }
    }
}

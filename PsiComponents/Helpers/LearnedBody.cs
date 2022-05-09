using Microsoft.Azure.Kinect.BodyTracking;
using MathNet.Numerics.Statistics;

namespace Helpers
{
    public class LearnedBody
    {
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), double> LearnedBones { get; private set; }
        public uint Id { get; private set; }
        public DateTime LastSeen { get; set; }
        public LearnedBody(uint id, Dictionary<(JointId ChildJoint, JointId ParentJoint), double> bones)
        {
            Id = id;
            LearnedBones = bones;
        }
        public bool IsSameAs(LearnedBody b, double maxDeviation)
        {
            return ProcessDifference(b) < maxDeviation;
        }
        private double ProcessDifference(LearnedBody b)
        {
            List<double> diff = new List<double>();
            foreach (var iterator in LearnedBones)
                if (iterator.Value > 0.0 && b.LearnedBones[iterator.Key] > 0.0)
                    diff.Add(Math.Abs(iterator.Value - b.LearnedBones[iterator.Key]));
            var statistics = Statistics.MeanStandardDeviation(diff);
            return statistics.Item2;
        }
        public uint FindClosest(List<LearnedBody> listOfBodies, double maxDeviation)
        {
            List<KeyValuePair<double, LearnedBody>> pairs = new List<KeyValuePair<double, LearnedBody>>();
            foreach (var pair in listOfBodies)
                pairs.Add(new KeyValuePair<double, LearnedBody>(ProcessDifference(pair), pair));
            pairs.Sort(new TupleDoubleLearnedBodyComparer());
            if (maxDeviation < pairs.First().Key)
                return 0;
            return pairs.First().Value.Id;
        }
    }

    internal class TupleDoubleLearnedBodyComparer : Comparer<KeyValuePair<double, LearnedBody>>
    {
        public override int Compare(KeyValuePair<double, LearnedBody> a, KeyValuePair<double, LearnedBody> b)
        {
            if (a.Key == b.Key)
                return 0;
            return a.Key > b.Key ? 1 : -1;
        }
    }
}

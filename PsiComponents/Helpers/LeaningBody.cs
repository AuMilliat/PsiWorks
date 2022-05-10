﻿using Microsoft.Azure.Kinect.BodyTracking;
using MathNet.Numerics.Statistics;

namespace Helpers
{
    public class LearningBody
    {
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>> LearningBones { get; set; }
        public uint Id { get; private set; }
        public DateTime CreationTime { get; private set; }

        public LearningBody(uint id, DateTime time, List<(JointId ChildJoint, JointId ParentJoint)> bones)
        {
            Id = id;
            CreationTime = time;
            LearningBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>>();
            foreach (var bone in bones)
                LearningBones.Add(bone, new List<double>());
        }
        public bool StillLearning(DateTime time, TimeSpan duration)
        {
            return (time - CreationTime) < duration;
        }
        public LearnedBody GeneratorLearnedBody(double maxStdDev)
        {
            Dictionary<(JointId ChildJoint, JointId ParentJoint), double> learnedBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), double>();
            foreach (var iterator in LearningBones)
            {
                var statistics = Statistics.MeanStandardDeviation(iterator.Value);
                if (statistics.Item2 < maxStdDev)
                    learnedBones[iterator.Key] = statistics.Item1;
                else
                    learnedBones[iterator.Key] = -1;
            }
            return new LearnedBody(Id, learnedBones);
        }
    }
}
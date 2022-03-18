using Microsoft.Azure.Kinect.BodyTracking;

namespace Helpers
{
    public class SimplifiedBody
    {
        public uint Id { get; set; } = uint.MaxValue;
        public enum SensorOrigin { Nuitrack, Azure };

        public SensorOrigin Origin { get; private set; }
        public Dictionary<JointId, Tuple<JointConfidenceLevel, System.Numerics.Vector3>> Joints { get; set; }

        public SimplifiedBody(SensorOrigin origin, uint id, Dictionary<JointId, Tuple<JointConfidenceLevel, System.Numerics.Vector3>>? joints = null)
        {
            Origin = origin;
            Id = id;
            if(joints == null)
                Joints = new Dictionary<JointId, Tuple<JointConfidenceLevel, System.Numerics.Vector3>>();
            else
                Joints = joints;
        }
    }
}

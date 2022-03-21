using MathNet.Spatial.Euclidean;

namespace NatNetComponent
{
    public struct RigidBody
    {
        public string name;
        public Vector3D position;
        public Quaternion orientation;
    }

    public struct Joint
    {
        public uint id;
        public float confidence;
        public Vector3D position;
        public Quaternion orientation;
    }
    public struct Skeleton
    {
        public uint id;
        public List<Joint> body;
    }
}
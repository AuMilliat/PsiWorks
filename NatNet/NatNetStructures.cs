namespace NatNetComponent
{
    public struct RigidBody
    {
        public string name;
        public System.Numerics.Vector3 position;
        public System.Numerics.Quaternion orientation;
    }

    public struct Joint
    {
        public uint id;
        public float confidence;
        public System.Numerics.Vector3 position;
        public System.Numerics.Quaternion orientation;
    }
    public struct Skeleton
    {
        public uint id;
        public List<Joint> body;
    }
}
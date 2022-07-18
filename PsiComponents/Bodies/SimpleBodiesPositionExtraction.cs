using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using MathNet.Spatial.Euclidean;
using nuitrack;

namespace Bodies
{
    public class SimpleBodiesPositionExtractionConfiguration
    {
        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Nuitrack.
        /// </summary>
        public JointType NuitrackJointAsPosition { get; set; } = JointType.Torso;

        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Nuitrack.
        /// </summary>
        public Microsoft.Azure.Kinect.BodyTracking.JointId AzureJointAsPosition { get; set; } = Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis;
    }
    public class SimpleBodiesPositionExtraction : Subpipeline
    {

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, Vector3D>> OutBodiesPositions { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<Skeleton>> InBodiesNuitrackConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<Skeleton>> InBodiesNuitrack => InBodiesNuitrackConnector.In;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<AzureKinectBody>> InBodiesAzureConnector;

        // Receiver that encapsulates the input list of Azure skeletons
        public Receiver<List<AzureKinectBody>> InBodiesAzure => InBodiesAzureConnector.In;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<Helpers.SimplifiedBody>> InBodiesSimplifiedConnector;

        // Receiver that encapsulates the input list of Azure skeletons
        public Receiver<List<Helpers.SimplifiedBody>> InBodiesSimplified => InBodiesSimplifiedConnector.In;

        private SimpleBodiesPositionExtractionConfiguration Configuration { get; }

        public SimpleBodiesPositionExtraction(Pipeline parent, SimpleBodiesPositionExtractionConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new SimpleBodiesPositionExtractionConfiguration();
            InBodiesNuitrackConnector = CreateInputConnectorFrom<List<Skeleton>>(parent, nameof(InBodiesNuitrackConnector));
            InBodiesAzureConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(parent, nameof(InBodiesAzureConnector));
            InBodiesSimplifiedConnector = CreateInputConnectorFrom<List<Helpers.SimplifiedBody>>(parent, nameof(InBodiesSimplifiedConnector));
            OutBodiesPositions = parent.CreateEmitter<Dictionary<uint, Vector3D>>(this, nameof(OutBodiesPositions));
            InBodiesAzureConnector.Out.Do(Process);
            InBodiesNuitrackConnector.Out.Do(Process);
            InBodiesSimplifiedConnector.Out.Do(Process);
        }

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add((uint)skeleton.ID, Helpers.Helpers.NuitrackToMathNet(skeleton.GetJoint(Configuration.NuitrackJointAsPosition).Real));
            OutBodiesPositions.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add(skeleton.TrackingId, skeleton.Joints[Configuration.AzureJointAsPosition].Pose.Origin.ToVector3D());
            OutBodiesPositions.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<Helpers.SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                if(!skeletons.ContainsKey(skeleton.Id))
                    skeletons.Add(skeleton.Id, skeleton.Joints[Configuration.AzureJointAsPosition].Item2);
            OutBodiesPositions.Post(skeletons, envelope.OriginatingTime);
        }
    }
}

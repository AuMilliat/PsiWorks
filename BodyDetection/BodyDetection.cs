using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using nuitrack;

namespace BodyDetection
{
    public class BodyDetectionConfiguration
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
    public class BodyDetection : Subpipeline
    {

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, System.Numerics.Vector3>> OutBodiesPositions { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<Skeleton>> BodiesNuitrackInConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<Skeleton>> NuitrackBodiesIn => BodiesNuitrackInConnector.In;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<AzureKinectBody>> BodiesAzureInConnector;

        // Receiver that encapsulates the input list of Azure skeletons
        public Receiver<List<AzureKinectBody>> BodiesAzureIn => BodiesAzureInConnector.In;

        private BodyDetectionConfiguration Configuration { get; }

        public BodyDetection(Pipeline parent, BodyDetectionConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            if (configuration == null)
                Configuration = new BodyDetectionConfiguration();
            else
                Configuration = configuration;
            BodiesNuitrackInConnector = CreateInputConnectorFrom<List<Skeleton>>(parent, nameof(BodiesNuitrackInConnector));
            BodiesAzureInConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(parent, nameof(BodiesAzureInConnector));
            OutBodiesPositions = parent.CreateEmitter<Dictionary<uint, System.Numerics.Vector3>>(this, nameof(OutBodiesPositions));
            BodiesAzureInConnector.Out.Do(Process);
            BodiesNuitrackInConnector.Out.Do(Process);
        }

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            Dictionary<uint, System.Numerics.Vector3> skeletons = new Dictionary<uint, System.Numerics.Vector3>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add((uint)skeleton.ID, Helpers.Helpers.NuitrackToSystem(skeleton.GetJoint(Configuration.NuitrackJointAsPosition).Real));
            }
            OutBodiesPositions.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            Dictionary<uint, System.Numerics.Vector3> skeletons = new Dictionary<uint, System.Numerics.Vector3>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add(skeleton.TrackingId, Helpers.Helpers.AzureToSystem(skeleton.Joints[Configuration.AzureJointAsPosition].Pose.Origin));
            }
            OutBodiesPositions.Post(skeletons, envelope.OriginatingTime);
        }
    }
}

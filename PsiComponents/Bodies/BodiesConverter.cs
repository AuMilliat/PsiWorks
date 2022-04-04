using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using MathNet.Spatial.Euclidean;
using nuitrack;

namespace Bodies
{
    public class BodiesConverter : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<Helpers.SimplifiedBody>> OutBodies { get; private set; }

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

        private Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId> NuiToAzure;
        public BodiesConverter(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            InBodiesNuitrackConnector = CreateInputConnectorFrom<List<Skeleton>>(parent, nameof(InBodiesNuitrackConnector));
            InBodiesAzureConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(parent, nameof(InBodiesAzureConnector));
            OutBodies = parent.CreateEmitter<List<Helpers.SimplifiedBody>>(this, nameof(OutBodies));
            InBodiesAzureConnector.Out.Do(Process);
            InBodiesNuitrackConnector.Out.Do(Process);
            NuiToAzure = new Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId>();
            NuiToAzure.Add(JointType.Head, Microsoft.Azure.Kinect.BodyTracking.JointId.Head);
            NuiToAzure.Add(JointType.Neck, Microsoft.Azure.Kinect.BodyTracking.JointId.Neck);
            NuiToAzure.Add(JointType.Torso, Microsoft.Azure.Kinect.BodyTracking.JointId.SpineChest);
            NuiToAzure.Add(JointType.Waist, Microsoft.Azure.Kinect.BodyTracking.JointId.SpineNavel);
            NuiToAzure.Add(JointType.LeftCollar, Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleLeft);
            NuiToAzure.Add(JointType.LeftShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderLeft);
            NuiToAzure.Add(JointType.LeftElbow, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowLeft);
            NuiToAzure.Add(JointType.LeftWrist, Microsoft.Azure.Kinect.BodyTracking.JointId.WristLeft);
            NuiToAzure.Add(JointType.LeftHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandLeft);
            NuiToAzure.Add(JointType.LeftFingertip, Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipLeft);
            NuiToAzure.Add(JointType.RightCollar, Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleRight);
            NuiToAzure.Add(JointType.RightShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderRight);
            NuiToAzure.Add(JointType.RightElbow, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowRight);
            NuiToAzure.Add(JointType.RightWrist, Microsoft.Azure.Kinect.BodyTracking.JointId.WristRight);
            NuiToAzure.Add(JointType.RightHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandRight);
            NuiToAzure.Add(JointType.RightFingertip, Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipRight);
            NuiToAzure.Add(JointType.LeftHip, Microsoft.Azure.Kinect.BodyTracking.JointId.HipLeft);
            NuiToAzure.Add(JointType.LeftKnee, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeLeft);
            NuiToAzure.Add(JointType.LeftAnkle, Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleLeft);
            NuiToAzure.Add(JointType.LeftFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootLeft);
            NuiToAzure.Add(JointType.RightHip, Microsoft.Azure.Kinect.BodyTracking.JointId.HipRight);
            NuiToAzure.Add(JointType.RightKnee, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeRight);
            NuiToAzure.Add(JointType.RightAnkle, Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleRight);
            NuiToAzure.Add(JointType.RightFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootRight);
        }

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            List<Helpers.SimplifiedBody> returnedBodies = new List<Helpers.SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                Helpers.SimplifiedBody body = new Helpers.SimplifiedBody(Helpers.SimplifiedBody.SensorOrigin.Azure, (uint)skeleton.ID);
                foreach (var joint in skeleton.Joints)
                    body.Joints.Add(NuiToAzure[joint.Type], new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel,   Vector3D>
                        (Helpers.Helpers.FloatToConfidence(joint.Confidence), Helpers.Helpers.NuitrackToMathNet(joint.Real)));
                returnedBodies.Add(body);
            }
            OutBodies.Post(returnedBodies, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            List<Helpers.SimplifiedBody> returnedBodies = new List<Helpers.SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                Helpers.SimplifiedBody body = new Helpers.SimplifiedBody(Helpers.SimplifiedBody.SensorOrigin.Azure, skeleton.TrackingId);
                foreach (var joint in skeleton.Joints)
                    body.Joints.Add(joint.Key, new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel,   Vector3D>
                        (joint.Value.Confidence, joint.Value.Pose.Origin.ToVector3D()));
                returnedBodies.Add(body);
            }
            OutBodies.Post(returnedBodies, envelope.OriginatingTime);
        }
    }
}

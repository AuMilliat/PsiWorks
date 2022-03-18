using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using nuitrack;
using Helpers;

namespace BodiesDetection
{
    public class BodiesDetectionConfiguration
    {
        /// <summary>
        /// Gets or sets do the calibration run first ?
        /// </summary>
        public bool DoCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets while calibreating the component is sending bodies while calibrating.
        /// The camera is selected with SendBodiesInCamera1Space parameter.
        /// </summary>
        public bool SendBodiesDuringCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of joints used in ransac for calibration.
        /// </summary>
        public uint NumberOfJoint { get; set; } = 200;

        /// <summary>
        /// Gets or sets the confidence level used for calibration.
        /// </summary>
        public Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.High;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public bool SendBodiesInCamera1Space { get; set; } = true;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public System.Numerics.Matrix4x4 Camera2ToCamera1Transformation { get; set; } = System.Numerics.Matrix4x4.Identity;
    }
    public class BodiesDetection : Subpipeline
    {

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesCalibrated{ get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        private BodiesDetectionConfiguration Configuration { get; }

        public BodiesDetection(Pipeline parent, BodiesDetectionConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            if (configuration == null)
                Configuration = new BodiesDetectionConfiguration();
            else
                Configuration = configuration;
            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            OutBodiesCalibrated = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesCalibrated));
            InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            if(Configuration.SendBodiesInCamera1Space)
                OutBodiesCalibrated.Post(bodies.Item1, envelope.OriginatingTime);
            else
                OutBodiesCalibrated.Post(bodies.Item2, envelope.OriginatingTime);
        }

    }
}

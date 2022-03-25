using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;
using Helpers;

namespace BodiesDetection
{
    public class BodiesDetectionConfiguration
    {
        /// <summary>
        /// Gets or sets while calibreating the component is sending bodies while calibrating.
        /// The camera is selected with SendBodiesInCamera1Space parameter.
        /// </summary>
        public bool SendBodiesDuringCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public bool SendBodiesInCamera1Space { get; set; } = true;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public Matrix<double>? Camera2ToCamera1Transformation { get; set; } = null;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public JointId JointUsedForCorrespondence { get; set; } = JointId.Pelvis;
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

            if (Configuration.SendBodiesInCamera1Space)
                OutBodiesCalibrated.Post(bodies.Item1, envelope.OriginatingTime);
            else
                OutBodiesCalibrated.Post(bodies.Item2, envelope.OriginatingTime);
        }

        private Dictionary<uint, uint> ComputeCorrespondenceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2) 
        {
            Dictionary<uint, uint> correspondanceMap = new Dictionary<uint, uint>();
            List<Vector3D> positionCamera1 = new List<Vector3D>();
            foreach (SimplifiedBody body in camera1)
            {
                positionCamera1.Add(body.Joints[Configuration.JointUsedForCorrespondence].Item2);
            }
            List<Vector3D> positionCamera2 = new List<Vector3D>();
            foreach (SimplifiedBody body in camera2)
            {
                positionCamera2.Add(CalculateTransform(body.Joints[Configuration.JointUsedForCorrespondence].Item2));
            }
            return correspondanceMap;
        }

        private Vector3D CalculateTransform(Vector3D origin)
        {
            Vector<double> v4Origin = Vector<double>.Build.DenseOfVector(origin.ToVector());
            v4Origin[3] = 1.0f;
            var result = v4Origin * Configuration.Camera2ToCamera1Transformation;
            return new Vector3D(result[0], result[1], result[2]);

        }
    }
}

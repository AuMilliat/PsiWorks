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
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public Matrix<double>? Camera2ToCamera1Transformation { get; set; } = null;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public JointId JointUsedForCorrespondence { get; set; } = JointId.Pelvis;

        /// <summary>
        /// Gets or sets maximum acceptable distance for correpondance in millimeter
        /// </summary>
        public double MaxDistance { get; set; } = 80.0;

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
        private Connector<Matrix<double>> InCalibrationMatrixConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<Matrix<double>> InCalibrationMatrix => InCalibrationMatrixConnector.In;

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
            Configuration = configuration ?? new BodiesDetectionConfiguration();

            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            InCalibrationMatrixConnector = CreateInputConnectorFrom<Matrix<double>>(parent, nameof(InCalibrationMatrixConnector));
            OutBodiesCalibrated = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesCalibrated));

            if (Configuration.Camera2ToCamera1Transformation == null)
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Out.Fuse(InCalibrationMatrixConnector.Out, Available.Nearest<Matrix<double>>()).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);
        }
        private void Process((List<SimplifiedBody>, List<SimplifiedBody>, Matrix<double>) bodies, Envelope envelope)
        {
            Configuration.Camera2ToCamera1Transformation = bodies.Item3;
            Process((bodies.Item1, bodies.Item2), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Dictionary<uint, SimplifiedBody> dicsC1 = new Dictionary<uint, SimplifiedBody>(), dicsC2 = new Dictionary<uint, SimplifiedBody>();
            OutBodiesCalibrated.Post(SelectBestBody(dicsC1, dicsC2, ComputeCorrespondenceMap(bodies.Item1, bodies.Item2, ref dicsC1, ref dicsC2)), envelope.OriginatingTime);
        }

        private List<Tuple<uint, uint>> ComputeCorrespondenceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2) 
        {
            // Bruteforce ftm, might simplify to check directly the max allowed distance.
            Dictionary<uint, List<Tuple<double, uint>>> distances = new Dictionary<uint, List<Tuple<double, uint>>>();
            foreach (SimplifiedBody bodyC1 in camera1)
            {
                d1[bodyC1.Id] = bodyC1;
                foreach (SimplifiedBody bodyC2 in camera2)
                    distances[bodyC1.Id].Add(new Tuple<double, uint>(MathNet.Numerics.Distance.SSD(bodyC1.Joints[Configuration.JointUsedForCorrespondence].Item2.ToVector(), Helpers.Helpers.CalculateTransform(bodyC2.Joints[Configuration.JointUsedForCorrespondence].Item2, Configuration.Camera2ToCamera1Transformation).ToVector()), bodyC2.Id));
            }

            List<Tuple<uint, uint>> correspondanceMap = new List<Tuple<uint, uint>>();
            List<uint> notMissingC2 = new List<uint>();
            foreach(var iterator in distances)
            {
                iterator.Value.Sort(new TupleDoubleUintComparer());
                //to check if sort is good
                if (iterator.Value.First().Item1 < Configuration.MaxDistance)
                {
                    correspondanceMap.Add(new Tuple<uint, uint>(iterator.Key, iterator.Value.First().Item2));
                    notMissingC2.Add(iterator.Value.First().Item2);
                }
                else
                    correspondanceMap.Add(new Tuple<uint, uint>(iterator.Key, 0));
            }
            foreach(SimplifiedBody bodyC2 in camera2)
            {
                d1[bodyC2.Id] = bodyC2;
                if (!notMissingC2.Contains(bodyC2.Id))
                    correspondanceMap.Add(new Tuple<uint, uint>(0, bodyC2.Id));
            }
            return correspondanceMap;
        }

        private List<SimplifiedBody> SelectBestBody(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, List<Tuple<uint, uint>> pairing)
        {
            List<SimplifiedBody> bestBodies = new List<SimplifiedBody>();
            foreach(var pair in pairing)
            {
                if (pair.Item2 == 0)
                    bestBodies.Add(camera1[pair.Item1]);
                else if (pair.Item1 == 0)
                    bestBodies.Add(TransformBody(camera2[pair.Item2]));
                else
                {
                    if (AccumulatedConfidence(camera1[pair.Item1]) < AccumulatedConfidence(camera2[pair.Item2]))
                        bestBodies.Add(camera1[pair.Item1]);
                    else
                        bestBodies.Add(TransformBody(camera2[pair.Item2]));
                }
            }
            return bestBodies;
        } 
        
        private double AccumulatedConfidence(SimplifiedBody body)
        {
            //might use coef for usefull joints.
            int accumulator = 0;
            foreach(var joint in body.Joints)
                accumulator+=(int)joint.Value.Item1;
            return accumulator;
        }

        private SimplifiedBody TransformBody(SimplifiedBody body)
        {
            SimplifiedBody transformed = body;
            foreach (var joint in body.Joints)
                transformed.Joints[joint.Key] = new Tuple<JointConfidenceLevel, Vector3D>(joint.Value.Item1, Helpers.Helpers.CalculateTransform(joint.Value.Item2, Configuration.Camera2ToCamera1Transformation)); ;
            return transformed;
        }
    }

    internal class TupleDoubleUintComparer : Comparer<Tuple<double, uint>>
    {
        public override int Compare(Tuple<double, uint> a, Tuple<double, uint> b)
        {
            if(a.Item1 == b.Item1)
                return 0;
            return a.Item1 > b.Item1 ? 1 : -1;
        }
    }
}

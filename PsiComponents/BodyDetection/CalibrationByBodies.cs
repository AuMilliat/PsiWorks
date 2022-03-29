using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;
using System.IO;
using Helpers;

namespace CalibrationByBodies
{
    public class CalibrationByBodiesConfiguration
    {
        /// <summary>
        /// Gets or sets the number of joints used in ransac for calibration.
        /// </summary>
        public uint NumberOfJoint { get; set; } = 200;

        /// <summary>
        /// Gets or sets the confidence level used for calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.High;

        /// <summary>
        /// Test the transformation matrix with some frames & AllowedMaxStdDeviation.
        /// </summary>
        public bool TestMatrixBeforeSending { get; set; } = true;

        /// <summary>
        /// .
        /// </summary>
        public double AllowedMaxStdDeviation { get; set; } = 0.1;

        /// <summary>
        /// Connect Synch event receiver
        /// </summary>
        public bool SynchedCalibration { get; set; } = true;

        /// <summary>
        /// Pouet Status.
        /// </summary>
        public delegate void DelegateStatus(string status);
        public DelegateStatus? SetStatus = null;

        /// <summary>
        /// Pouet Status.
        /// </summary>
        public string StoringPath { get; set; } = "./Calib.csv";
    }
    public class CalibrationByBodies : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Matrix<double>> OutCalibration { get; private set; }

        /// <summary>
        /// Synch signals for capturing skeletons.
        /// </summary>
        private Connector<bool> InSynchEventConnector;

        // Receiver that encapsulates the synch signal.
        public Receiver<bool> InSynchEvent => InSynchEventConnector.In;

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

        private CalibrationByBodiesConfiguration Configuration { get; }

        //Calibration stuff
        private Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]> CalibrationJoints;
        private DateTime? CalibrationTime = null;
        private int JointAddedCount = 0;
        private enum ECalibrationState { Idle, Running, Testing };
        private ECalibrationState CalibrationState = ECalibrationState.Running;
        Matrix<double> TransformationMatrix = Matrix<double>.Build.Dense(1,1);
        private double[] TestingArray;

        public CalibrationByBodies(Pipeline parent, CalibrationByBodiesConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            if (configuration == null)
                Configuration = new CalibrationByBodiesConfiguration();
            else
                Configuration = configuration;
            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            OutCalibration = parent.CreateEmitter<Matrix<double>>(this, nameof(OutCalibration));
            
            if(Configuration.SynchedCalibration)
            {
                InSynchEventConnector = CreateInputConnectorFrom<bool>(parent, nameof(InSynchEventConnector));
                InSynchEventConnector.Pair(InCamera1BodiesConnector).Pair(InCamera2BodiesConnector).Do(Process);
            }
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            Emgu.CV.Structure.MCvPoint3D32f[] camera1 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJoint];
            Emgu.CV.Structure.MCvPoint3D32f[] camera2 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJoint];
            CalibrationJoints = new Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]>(camera1, camera2);
            TestingArray = new double[Configuration.NumberOfJoint];
            Configuration.SetStatus("Collecting data...");
        }

        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Process((bodies.Item2, bodies.Item3), envelope);
        }
        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            switch(CalibrationState)
            {
                case ECalibrationState.Running:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        CalibrationState = DoCalibration(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
                case ECalibrationState.Testing:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        CalibrationState = DoTesting(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
            }
        }

        private ECalibrationState DoCalibration(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            //Wait 5 seconds
            if (CalibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - CalibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Running;
            }
            CalibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (JointAddedCount >= Configuration.NumberOfJoint)
                        break;
                    CalibrationJoints.Item1[JointAddedCount] = VectorToCVPoint(camera1.Joints[iterator].Item2);
                    CalibrationJoints.Item2[JointAddedCount] = VectorToCVPoint(camera2.Joints[iterator].Item2);
                    JointAddedCount++;
                }
            }
            Configuration.SetStatus("Calibration running:  " + JointAddedCount.ToString() + "/" + Configuration.NumberOfJoint.ToString());
            if (JointAddedCount >= Configuration.NumberOfJoint)
            {
                Emgu.CV.UMat outputArray = new Emgu.CV.UMat();
                Emgu.CV.UMat inliers = new Emgu.CV.UMat();
                Emgu.CV.Util.VectorOfPoint3D32F v1 = new Emgu.CV.Util.VectorOfPoint3D32F();
                Emgu.CV.Util.VectorOfPoint3D32F v2 = new Emgu.CV.Util.VectorOfPoint3D32F();
                v1.Push(CalibrationJoints.Item1);
                v2.Push(CalibrationJoints.Item2);
                int retval = Emgu.CV.CvInvoke.EstimateAffine3D(v1, v2, outputArray, inliers);

                var enumerator = outputArray.GetOutputArray().GetMat().GetData().GetEnumerator();
                double[,] dArray = new double[4, 4];
                int index = 0;
                while (enumerator.MoveNext())
                {
                    dArray[index%4, index/4] = (double)enumerator.Current;
                    index++;
                }

                dArray[3, 0] = dArray[3, 1] = dArray[3, 2] = 0.0;
                dArray[3, 3] = 1.0;
                TransformationMatrix = Matrix<double>.Build.DenseOfArray(dArray);
                CleanIteratorsAndCounters();
                if(Configuration.TestMatrixBeforeSending)
                {
                    Configuration.SetStatus("Calibration done! Checking...");
                    return ECalibrationState.Testing;
                }
                else
                {
                    OutCalibration.Post(TransformationMatrix, time);
                    Configuration.SetStatus("Calibration Done");
                    StoreCalibrationMatrix();
                    return ECalibrationState.Idle;
                }
            }
            return ECalibrationState.Running;
        }

        private ECalibrationState DoTesting(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            if (CalibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - CalibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Testing;
            }
            CalibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (JointAddedCount >= Configuration.NumberOfJoint)
                        break;
                    TestingArray[JointAddedCount++] = MathNet.Numerics.Distance.SSD(camera1.Joints[iterator].Item2.ToVector(), CalculateTransform(camera2.Joints[iterator].Item2).ToVector());
                }
            }
            Configuration.SetStatus("Checking: " + JointAddedCount.ToString() + "/" + Configuration.NumberOfJoint.ToString());

            if (JointAddedCount >= Configuration.NumberOfJoint)
            {
                var statistics = Statistics.MeanStandardDeviation(TestingArray);
                CleanIteratorsAndCounters();
                if (statistics.Item2 < Configuration.AllowedMaxStdDeviation)
                {
                    Configuration.SetStatus("Calibration done! StdDev: " + statistics.Item2.ToString());
                    OutCalibration.Post(TransformationMatrix, time);
                    StoreCalibrationMatrix();
                    return ECalibrationState.Idle;
                }
                else
                {
                    Configuration.SetStatus("Calibration running, StdDev: " + statistics.Item2.ToString());
                    return ECalibrationState.Running;
                }
            }
            return ECalibrationState.Testing;
        }

        private Emgu.CV.Structure.MCvPoint3D32f VectorToCVPoint(Vector3D point)
        {
            Emgu.CV.Structure.MCvPoint3D32f retValue = new Emgu.CV.Structure.MCvPoint3D32f((float)point.X, (float)point.Y, (float)point.Z);
            return retValue;
        }

        private Vector3D CalculateTransform(Vector3D origin)
        {
            Vector<double> v4Origin = Vector<double>.Build.Dense(4);
            v4Origin[0] = origin.X;
            v4Origin[1] = origin.Y;
            v4Origin[2] = origin.Z;
            v4Origin[3] = 1.0f;
            var result = v4Origin * TransformationMatrix;
            return new Vector3D(result[0], result[1], result[2]);
        }

        private void CleanIteratorsAndCounters()
        {
            CalibrationTime = null;
            JointAddedCount = 0;
        }

        private void StoreCalibrationMatrix()
        {
            if(Configuration.StoringPath.Length > 4)
                File.WriteAllText(Configuration.StoringPath, TransformationMatrix.ToMatrixString());
        }
    }
}

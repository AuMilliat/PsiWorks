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
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.High;

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

        //Calibration stuff
        private Tuple<List< Vector3D>, List< Vector3D>>? CalibrationJoints = null;
        private DateTime? CalibrationTime = null;

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
            if(Configuration.DoCalibration)
            {
                List<Vector3D> camera1 = new List<Vector3D>();
                List<Vector3D> camera2 = new List<Vector3D>();
                CalibrationJoints = new Tuple<List<Vector3D>, List<Vector3D>>(camera1, camera2);
            }
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            if (Configuration.DoCalibration && bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
            {
                Configuration.DoCalibration = DoCalibration(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                if (Configuration.SendBodiesDuringCalibration)
                {
                    if (Configuration.SendBodiesInCamera1Space)
                        OutBodiesCalibrated.Post(bodies.Item1, envelope.OriginatingTime);
                    else
                        OutBodiesCalibrated.Post(bodies.Item2, envelope.OriginatingTime);
                }
                else
                    return;
            }
          
            
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

            //foreach (Vector3D )

            return correspondanceMap;
        }
        private bool DoCalibration(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            //Wait 5 seconds
            if (CalibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - CalibrationTime);
                if(interval.TotalMilliseconds < 5000)
                    return false;
            }
            CalibrationTime = time;
            if (CalibrationJoints == null)
            {
                List<Vector3D> lCamera1 = new List<Vector3D>();
                List<Vector3D> lCamera2 = new List<Vector3D>();
                CalibrationJoints = new Tuple<List<Vector3D>, List<Vector3D>>(lCamera1, lCamera2);
            }
            for(JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    CalibrationJoints.Item1.Add(camera1.Joints[iterator].Item2);
                    CalibrationJoints.Item2.Add(camera1.Joints[iterator].Item2);
                }
            }
            if (CalibrationJoints.Item1.Count >= Configuration.NumberOfJoint)
            {
                return true;
            }
            return false;
        }

        //public static void ICP_run()
        //{
        //    int Np;
        //    var m = Matrix<double>.Build;
        //    Np = P_points.ColumnCount;
        //    Matrix<double> Y;
        //    Y = KD_tree(M_points, P_points);
        //    double s = 1;

        //    Matrix<double> R;
        //    Matrix<double> t;
        //     Vector3D d;
        //    double err = 0;
        //    Matrix<double> dummy_Row = m.Dense(1, Np, 0);
        //    ///Nokta sayilari

        //    ///P ve Y matrislerinin agirlik merkezi hesaplaniyor
        //    Matrix<double> Mu_p = FindCentroid(P_points);
        //    Matrix<double> Mu_y = FindCentroid(Y);

        //    Matrix<double> dummy_p1 = m.Dense(1, Np);
        //    Matrix<double> dummy_p2 = m.Dense(1, Np);
        //    Matrix<double> dummy_p3 = m.Dense(1, Np, 0);
        //    Matrix<double> dummy_y1 = m.Dense(1, Np);
        //    Matrix<double> dummy_y2 = m.Dense(1, Np);
        //    Matrix<double> dummy_y3 = m.Dense(1, Np, 0);
        //    ///P matrisinin X ve Y koordinatlarini iceren satirlari farkli matrislere aliniyor
        //    dummy_p1.SetRow(0, P_points.Row(0));
        //    dummy_p2.SetRow(0, P_points.Row(1));
        //    dummy_p3.SetRow(0, P_points.Row(2));
            
        //    /// P deki her bir noktadan p nin agirlik merkezinin koordinatlari cikartiliyor(ZERO MEAN) yeni bir matris icerisine kaydediliyor.
        //    Matrix<double> P_prime = (dummy_p1 - Mu_p[0, 0]).Stack(dummy_p2 - Mu_p[1, 0]).Stack(dummy_p3 - Mu_p[2, 0]);
        //    ///Y matrisinin X ve Y koordinatlarini iceren satirlari farkli matrislere aliniyor
        //    dummy_y1.SetRow(0, Y.Row(0));
        //    dummy_y2.SetRow(0, Y.Row(1));
        //    dummy_y3.SetRow(0, Y.Row(2));
           
        //    /// P deki her bir noktadan p nin agirlik merkezinin koordinatlari cikartiliyor(ZERO MEAN) yeni bir matris icerisine kaydediliyor.
        //    Matrix<double> Y_prime = (dummy_y1 - Mu_y[0, 0]).Stack((dummy_y2 - Mu_y[1, 0]).Stack(dummy_y3 - Mu_y[2, 0]));
        //    /// -X -Y -Z koordinat matrisleri aliniyor.
        //    Matrix<double> Px = m.Dense(1, Np);
        //    Matrix<double> Py = m.Dense(1, Np);
        //    Matrix<double> Pz = m.Dense(1, Np, 0);
        //    Matrix<double> Yx = m.Dense(1, Np);
        //    Matrix<double> Yy = m.Dense(1, Np);
        //    Matrix<double> Yz = m.Dense(1, Np, 0);
        //    Px.SetRow(0, P_prime.Row(0));
        //    Py.SetRow(0, P_prime.Row(1));

        //    Yx.SetRow(0, Y_prime.Row(0));
        //    Yy.SetRow(0, Y_prime.Row(1));

        //    Pz.SetRow(0, P_prime.Row(2));
        //    Yz.SetRow(0, Y_prime.Row(2));
     

        //    var Sxx = Px * Yx.Transpose();
        //    var Sxy = Px * Yy.Transpose();
        //    var Sxz = Px * Yz.Transpose();

        //    var Syx = Py * Yx.Transpose();
        //    var Syy = Py * Yy.Transpose();
        //    var Syz = Py * Yz.Transpose();

        //    var Szx = Pz * Yx.Transpose();
        //    var Szy = Pz * Yy.Transpose();
        //    var Szz = Pz * Yz.Transpose();
        //    Matrix<double> Nmatrix = m.DenseOfArray(new[,]{{ Sxx[0, 0] + Syy[0, 0] + Szz[0, 0],  Syz[0, 0] - Szy[0, 0],       -Sxz[0, 0] + Szx[0, 0],        Sxy[0, 0] - Syx[0, 0]},
        //                                        {-Szy[0, 0] + Syz[0, 0],        Sxx[0, 0] - Syy[0, 0] - Szz[0, 0],  Sxy[0, 0] + Syx[0, 0],        Sxz[0, 0] + Szx[0, 0]},
        //                                        {Szx[0, 0] - Sxz[0, 0],         Syx[0, 0] + Sxy[0, 0],       -Sxx[0, 0] + Syy[0, 0] - Szz[0, 0],  Syz[0, 0] + Szy[0, 0]},
        //                                        {-Syx[0, 0] + Sxy[0, 0],        Szx[0, 0] + Sxz[0, 0],        Szy[0, 0] + Syz[0, 0],       -Sxx[0, 0] + Szz[0, 0] - Syy[0, 0]} });

        //    var evd = Nmatrix.Evd();
        //    Matrix<double> eigenvectors = evd.EigenVectors;
        //    var q = eigenvectors.Column(3);
        //    var q0 = q[0]; var q1 = q[1]; var q2 = q[2]; var q3 = q[3];

        //    ///Quernion matrislerinin bulunmasi
        //    var Qbar = m.DenseOfArray(new[,] { { q0, -q1, -q2, -q3 },
        //                                       { q1, q0, q3, -q2 },
        //                                       { q2, -q3, q0, q1 },
        //                                       { q3, q2, -q1, q0 }});

        //    var Q = m.DenseOfArray(new[,] {    { q0, -q1, -q2, -q3 },
        //                                       { q1, q0, -q3, q2 },
        //                                       { q2, q3, q0, -q1 },
        //                                       { q3, -q2, q1, q0 }});
        //    ///Rotasyon matrisi hesabi
        //    R = (Qbar.Transpose()).Multiply(Q);
        //    R = (R.RemoveColumn(0)).RemoveRow(0);

        //    ///Translation hesabi
        //    t = Mu_y - s * R * Mu_p;

        //    ///hata hesabi     
           
        //        for (int i = 0; i < Np; i++)
        //        {
        //            d = Y.Column(i).Subtract(P_points.Column(i));
        //            err += d[0] * d[0] + d[1] * d[1] + d[2] * d[2];
        //        }
            
        //    Tuple<Matrix<double>, Matrix<double>, double> ret = new Tuple<Matrix<double>, Matrix<double>, double>(R, t, err);
        //    return ret;
        //}

        private Vector3D CalculateTransform(Vector3D origin)
        {
            Vector<double> v4Origin = Vector<double>.Build.DenseOfVector(origin.ToVector());
            v4Origin[3] = 1.0f;
            var result = v4Origin * Configuration.Camera2ToCamera1Transformation;
            return new Vector3D(result[0], result[1], result[2]);

        }
    }
}

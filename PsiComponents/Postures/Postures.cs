using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Helpers;

namespace Postures
{
    public enum Postures {
        Unkow,
        Moving,
        Sitting,
        Jumping,
        Arm_Left_Pointing,
        Arm_Right_Pointing,
    };

    /// <summary>
    /// /////////////////////////////////////////////////TODO
    /// NEED ALL THRESHOLD IN CONFIGURATION
    /// </summary>
    public class SimplePostuesConfiguration
    { 
        /// <summary>
        /// Gets or sets the confidence level used for calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;

        public double SpeedWalkingThreshold { get; set; } = 0.025;
        public double JumpingThreshold { get; set; } = 0.5;

        public double PointingAngleThreshold { get; set; } = 15.0;
        public double SittingFactor { get; set; } = 2.0; 
        public double SittingAngleThreshold { get; set; } = 90.0;

        public int IncreaseFactor { get; set; } = 8;
        public int DecreaseFactor { get; set; } = 1;

    }
    public class SimplePostures : Subpipeline
    {
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;


        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<Postures>>> OutPostures { get; private set; }

        private SimplePostuesConfiguration Configuration;

        private delegate void DelegatedProcess(SimplifiedBody body, DateTime time);
        private DelegatedProcess InstantDelegatedProcess;
        public SimplePostures(Pipeline parent, SimplePostuesConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new SimplePostuesConfiguration();
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InBodiesConnector));

            OutPostures = parent.CreateEmitter<Dictionary<uint, List<Postures>>>(this, nameof(OutPostures));
            InBodiesConnector.Out.Do(Process);
            InstantDelegatedProcess += IsInstantWalking;
            InstantDelegatedProcess += IsInstantSitting;
            InstantDelegatedProcess += IsInstantJumping;
            InstantDelegatedProcess += IsInstantPointingLeft;
            InstantDelegatedProcess += IsInstantPointingRight;
        }

        private Dictionary<uint, BodyData> BodiesData = new Dictionary<uint, BodyData>();
        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, List<Postures>> postures = new Dictionary<uint, List<Postures>>();
            foreach (var body in bodies)
                postures.Add(body.Id, Process(body, envelope.OriginatingTime));
            OutPostures.Post(postures, envelope.OriginatingTime);
        }

        private List<Postures> Process(SimplifiedBody body, DateTime time)
        {
            // confidence check  here??
            List<Postures> retValue = new List<Postures>();
            if (!BodiesData.ContainsKey(body.Id))
                BodiesData.Add(body.Id, new BodyData(body.Joints[JointId.Pelvis].Item2, time));

            InstantDelegatedProcess(body, time);

            BodiesData[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
            BodiesData[body.Id].LastSeen = time;

            for (int iterator = 1; iterator < BodiesData[body.Id].Detected.Count; iterator++)
            {
                if (BodiesData[body.Id].Detected[(Postures)iterator] < 0)
                {
                   BodiesData[body.Id].Detected[(Postures)iterator]= 0;
                    continue;
                }
                else if (BodiesData[body.Id].Detected[(Postures)iterator] > 10)
                    retValue.Add((Postures)iterator);
            }
  
            return retValue;
        }

        private void IsInstantWalking(SimplifiedBody body, DateTime time)
        {
            //Checking only in 2D (X,Y) the plevis movement from last frame
            if (!CheckConfidenceLevel(body.Joints[JointId.Pelvis]))
                return;
            Vector2D newPelvis = new Vector2D(body.Joints[JointId.Pelvis].Item2.X, body.Joints[JointId.Pelvis].Item2.Y);
            Vector2D oldPelvis = new Vector2D(BodiesData[body.Id].LastPosition.X, BodiesData[body.Id].LastPosition.Y);
       
            double distance = MathNet.Numerics.Distance.SSD(newPelvis.ToVector(), oldPelvis.ToVector());
            TimeSpan duration = time - BodiesData[body.Id].LastSeen;
            if (duration.TotalSeconds > 0.0 && (distance / duration.TotalSeconds) > Configuration.SpeedWalkingThreshold)
                BodiesData[body.Id].Detected[Postures.Moving] += Configuration.IncreaseFactor;
            BodiesData[body.Id].Detected[Postures.Moving] -= Configuration.DecreaseFactor;
        }

        private void IsInstantSitting(SimplifiedBody body, DateTime time)
        {
            if (!(CheckConfidenceLevel(body.Joints[JointId.FootLeft]) && CheckConfidenceLevel(body.Joints[JointId.FootRight])
               && CheckConfidenceLevel(body.Joints[JointId.KneeLeft]) && CheckConfidenceLevel(body.Joints[JointId.KneeRight])
               && CheckConfidenceLevel(body.Joints[JointId.HipLeft]) && CheckConfidenceLevel(body.Joints[JointId.HipRight])
               && CheckConfidenceLevel(body.Joints[JointId.Pelvis])))
                return;

            UnitVector3D fkLeft = (body.Joints[JointId.FootLeft].Item2 - body.Joints[JointId.KneeLeft].Item2).Normalize();
            UnitVector3D khLeft = (body.Joints[JointId.KneeLeft].Item2 - body.Joints[JointId.HipLeft].Item2).Normalize();
            UnitVector3D fkRight = (body.Joints[JointId.FootRight].Item2 - body.Joints[JointId.KneeRight].Item2).Normalize();
            UnitVector3D khRight = (body.Joints[JointId.KneeRight].Item2 - body.Joints[JointId.HipRight].Item2).Normalize();
            MathNet.Spatial.Units.Angle leftAngle = fkLeft.AngleTo(khLeft);
            MathNet.Spatial.Units.Angle rightAngle = fkRight.AngleTo(khRight);

            double height = body.Joints[JointId.Pelvis].Item2.Z - body.Joints[JointId.FootLeft].Item2.Z;
            double distance = MathNet.Numerics.Distance.SSD(body.Joints[JointId.FootLeft].Item2.ToVector(), body.Joints[JointId.FootRight].Item2.ToVector()) * Configuration.SittingFactor;

            if (leftAngle.Degrees < Configuration.SittingAngleThreshold && rightAngle.Degrees < Configuration.SittingAngleThreshold && height < distance)
                BodiesData[body.Id].Detected[Postures.Sitting] += Configuration.IncreaseFactor;
            BodiesData[body.Id].Detected[Postures.Sitting] -= Configuration.DecreaseFactor;
        }

        private void IsInstantJumping(SimplifiedBody body, DateTime time)
        {
            if (!CheckConfidenceLevel(body.Joints[JointId.Pelvis]))
                return;
            double height = body.Joints[JointId.Pelvis].Item2.Z - BodiesData[body.Id].LastPosition.Z;
            TimeSpan duration = time - BodiesData[body.Id].LastSeen;
            if (duration.TotalSeconds > 0.0 && (height/duration.TotalSeconds) > Configuration.JumpingThreshold)
                BodiesData[body.Id].Detected[Postures.Jumping] += Configuration.IncreaseFactor;
            BodiesData[body.Id].Detected[Postures.Jumping] -= Configuration.DecreaseFactor;
        }
    

        private void IsInstantPointingLeft(SimplifiedBody body, DateTime time)
        {
            List<JointId> joints = new List<JointId>();
            joints.Add(JointId.HandLeft);
            joints.Add(JointId.ElbowLeft); 
            joints.Add(JointId.ShoulderLeft);
            BodiesData[body.Id].Detected[Postures.Arm_Left_Pointing] += IsInstantPointing(body, time,joints);
        }
        private void IsInstantPointingRight(SimplifiedBody body, DateTime time)
        {
            List<JointId> joints = new List<JointId>();
            joints.Add(JointId.HandRight);
            joints.Add(JointId.ElbowRight);
            joints.Add(JointId.ShoulderRight);
            BodiesData[body.Id].Detected[Postures.Arm_Right_Pointing] += IsInstantPointing(body, time, joints);
        }
        private int IsInstantPointing(SimplifiedBody body, DateTime time, List<JointId> joints)
        {
            if (!(CheckConfidenceLevel(body.Joints[joints[0]]) && CheckConfidenceLevel(body.Joints[joints[1]])
             && CheckConfidenceLevel(body.Joints[joints[2]])))
                return 0;

            UnitVector3D handElbow = (body.Joints[joints[0]].Item2 - body.Joints[joints[1]].Item2).Normalize();
            UnitVector3D elbowShoulder = (body.Joints[joints[1]].Item2 - body.Joints[joints[2]].Item2).Normalize();

            MathNet.Spatial.Units.Angle angle = handElbow.AngleTo(elbowShoulder);
            if (angle.Degrees < Configuration.PointingAngleThreshold)
                return  Configuration.IncreaseFactor;
            return -Configuration.DecreaseFactor;
        }
        private bool CheckConfidenceLevel(Tuple<JointConfidenceLevel, Vector3D> joint)
        {
            return joint.Item1 >= Configuration.ConfidenceLevel;
        }
    }
    
    internal class BodyData
    {
        public DateTime LastSeen = default(DateTime);
        public Vector3D LastPosition = new Vector3D();
        public Dictionary<Postures, int> Detected = new Dictionary<Postures, int>();

        public BodyData(Vector3D position, DateTime time)
        {
            LastPosition = position;
            LastSeen = time;
            Detected.Add(Postures.Arm_Left_Pointing, 0);
            Detected.Add(Postures.Arm_Right_Pointing, 0);
            Detected.Add(Postures.Moving, 0);
            Detected.Add(Postures.Jumping, 0);
            Detected.Add(Postures.Sitting, 0);
        }
    }
}
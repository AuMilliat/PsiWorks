using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Helpers;

namespace Postures
{
    public enum Postures {
        Unkow,
        Walking,
        Sitting,
        Jumping,
        Arm_Left_Pointing,
        Arm_Right_Pointing,
    };

    public class SimplePostuesConfiguration
    { 
        /// <summary>
      /// Gets or sets the confidence level used for calibration.
      /// </summary>
        public JointConfidenceLevel ConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;

    }
    public class SimplePostures : Subpipeline
    {
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;


        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, Postures>> OutPostures { get; private set; }

        private SimplePostuesConfiguration Configuration;
        public SimplePostures(Pipeline parent, SimplePostuesConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new SimplePostuesConfiguration();
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InBodiesConnector));

            OutPostures = parent.CreateEmitter<Dictionary<uint, Postures>>(this, nameof(OutPostures));
            InBodiesConnector.Out.Do(Process);
        }

        private Dictionary<uint, BodyData> BodiesData = new Dictionary<uint, BodyData>();
        private Dictionary<uint, List<uint>> formedEntryGroups = new Dictionary<uint, List<uint>>();
        private List<uint> fixedBodies = new List<uint>();
        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Postures> postures = new Dictionary<uint, Postures>();
            foreach (var body in bodies)
                postures.Add(body.Id, Process(body, envelope.OriginatingTime));
            OutPostures.Post(postures, envelope.OriginatingTime);
        }

        private Postures Process(SimplifiedBody body, DateTime time)
        {
            Postures retValue = Postures.Unkow;
            if (!BodiesData.ContainsKey(body.Id))
                BodiesData.Add(body.Id, new BodyData());
            if(IsInstantWalking(body, time))
                retValue = Postures.Walking;
            if (IsInstantSitting(body, time))
                retValue = Postures.Sitting;
            if (IsInstantJumping(body, time))
                retValue = Postures.Jumping;
            if (IsInstantPointingLeft(body, time))
                retValue = Postures.Arm_Left_Pointing;
            if (IsInstantPointingRight(body, time))
                retValue = Postures.Arm_Right_Pointing;
            // confidence check ??
            BodiesData[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
            BodiesData[body.Id].LastSeen = time;

            return retValue;
        }

        private bool IsInstantWalking(SimplifiedBody body, DateTime time)
        {
            //if (!(CheckConfidenceLevel(body.Joints[JointId.FootLeft]) && CheckConfidenceLevel(body.Joints[JointId.FootRight])
            //   && CheckConfidenceLevel(body.Joints[JointId.Pelvis])))
            //    return false;
            //UnitVector3D left = (body.Joints[JointId.FootLeft].Item2 - body.Joints[JointId.Pelvis].Item2).Normalize();
            //UnitVector3D right = (body.Joints[JointId.FootRight].Item2 - body.Joints[JointId.Pelvis].Item2).Normalize();
            //MathNet.Spatial.Units.Angle angle = left.AngleTo(right);

            //double distance = MathNet.Numerics.Distance.SSD(BodiesData[body.Id].LastPosition.ToVector(), body.Joints[JointId.Pelvis].Item2.ToVector());
            //TimeSpan duration = time - BodiesData[body.Id].LastSeen;
            //if (duration.TotalSeconds > 0.0 && (distance / duration.TotalSeconds) > 10.0)
            //    return true;
            //return false;

            //Checking only in 2D (X,Y) the plevis movement from last frame
            if (!CheckConfidenceLevel(body.Joints[JointId.Pelvis]))
                return false;
            Vector2D newPelvis = new Vector2D(body.Joints[JointId.Pelvis].Item2.X, body.Joints[JointId.Pelvis].Item2.Y);
            Vector2D oldPelvis = new Vector2D(BodiesData[body.Id].LastPosition.X, BodiesData[body.Id].LastPosition.Y);
       
            double distance = MathNet.Numerics.Distance.SSD(newPelvis.ToVector(), oldPelvis.ToVector());
            TimeSpan duration = time - BodiesData[body.Id].LastSeen;
            if (duration.TotalSeconds > 0.0 && (distance / duration.TotalMilliseconds) > 10.0)
                return true;
            return false;
        }

        private bool IsInstantSitting(SimplifiedBody body, DateTime time)
        {
            if (!(CheckConfidenceLevel(body.Joints[JointId.FootLeft]) && CheckConfidenceLevel(body.Joints[JointId.FootRight])
               && CheckConfidenceLevel(body.Joints[JointId.KneeLeft]) && CheckConfidenceLevel(body.Joints[JointId.KneeRight])
               && CheckConfidenceLevel(body.Joints[JointId.HipLeft]) && CheckConfidenceLevel(body.Joints[JointId.HipRight])
               && CheckConfidenceLevel(body.Joints[JointId.Pelvis])))
                return false;

            UnitVector3D fkLeft = (body.Joints[JointId.FootLeft].Item2 - body.Joints[JointId.KneeLeft].Item2).Normalize();
            UnitVector3D khLeft = (body.Joints[JointId.KneeLeft].Item2 - body.Joints[JointId.HipLeft].Item2).Normalize();
            UnitVector3D fkRight = (body.Joints[JointId.FootRight].Item2 - body.Joints[JointId.KneeRight].Item2).Normalize();
            UnitVector3D khRight = (body.Joints[JointId.KneeRight].Item2 - body.Joints[JointId.HipRight].Item2).Normalize();
            MathNet.Spatial.Units.Angle leftAngle = fkLeft.AngleTo(khLeft);
            MathNet.Spatial.Units.Angle rightAngle = fkRight.AngleTo(khRight);

            double height = body.Joints[JointId.Pelvis].Item2.Z - body.Joints[JointId.FootLeft].Item2.Z;
            double distance = MathNet.Numerics.Distance.SSD(body.Joints[JointId.FootLeft].Item2.ToVector(), body.Joints[JointId.FootRight].Item2.ToVector());

            if (leftAngle.Degrees < 90.0 && rightAngle.Degrees < 90.0 && height < distance)
                return true;
            return false;
        }

        private bool IsInstantJumping(SimplifiedBody body, DateTime time)
        {
            if (!CheckConfidenceLevel(body.Joints[JointId.Pelvis]))
                return false;
            double height = body.Joints[JointId.Pelvis].Item2.Z - BodiesData[body.Id].LastPosition.Z;
            TimeSpan duration = time - BodiesData[body.Id].LastSeen;
            if (duration.TotalSeconds > 0.0 && (height/duration.TotalMilliseconds) > 10.0)
                return true;
            return false;
        }

        private bool IsInstantPointingLeft(SimplifiedBody body, DateTime time)
        {
            List<JointId> joints = new List<JointId>();
            joints.Add(JointId.HandLeft);
            joints.Add(JointId.ElbowLeft); 
            joints.Add(JointId.ShoulderLeft);
            return IsInstantPointing(body, time,joints);
        }
        private bool IsInstantPointingRight(SimplifiedBody body, DateTime time)
        {
            List<JointId> joints = new List<JointId>();
            joints.Add(JointId.HandRight);
            joints.Add(JointId.ElbowRight);
            joints.Add(JointId.ShoulderRight);
            return IsInstantPointing(body, time, joints);
        }
        private bool IsInstantPointing(SimplifiedBody body, DateTime time, List<JointId> joints)
        {
            if (!(CheckConfidenceLevel(body.Joints[joints[0]]) && CheckConfidenceLevel(body.Joints[joints[1]])
             && CheckConfidenceLevel(body.Joints[joints[2]])))
                return false;

            UnitVector3D handElbow = (body.Joints[joints[0]].Item2 - body.Joints[joints[1]].Item2).Normalize();
            UnitVector3D elbowShoulder = (body.Joints[joints[1]].Item2 - body.Joints[joints[2]].Item2).Normalize();

            MathNet.Spatial.Units.Angle angle = handElbow.AngleTo(elbowShoulder);
            if (angle.Degrees > 175.0)
                return true;
            return false;
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
        public List<double> WalkingData = new List<double>();
        public List<double> WalkingTime = new List<double>();
    }
}

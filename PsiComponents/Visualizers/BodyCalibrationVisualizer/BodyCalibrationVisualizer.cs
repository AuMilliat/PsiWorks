using System.Drawing;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Azure.Kinect.BodyTracking;
using Helpers;
using Visualizer;


namespace BodyCalibrationVisualizer
{
    public abstract class BodyCalibrationVisualizer : BasicVisualizer
    {
        protected Connector<List<SimplifiedBody>> InBodiesMasterConnector;
        protected Connector<List<SimplifiedBody>> InBodiesSlaveConnector;
        protected Connector<Matrix<double>> InCalibrationSlaveConnector;

        public Receiver<List<SimplifiedBody>> InBodiesMaster => InBodiesMasterConnector.In;
        public Receiver<List<SimplifiedBody>> InBodiesSlave => InBodiesSlaveConnector.In;
        public Receiver<Matrix<double>> InCalibrationSlave => InCalibrationSlaveConnector.In;

        public Matrix<double>? Calibration { get; set; } = null;

        protected delegate Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> DelegateThatShouldBeLambda(Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> vector);
        protected DelegateThatShouldBeLambda? Lambda = null;

        public BodyCalibrationVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(pipeline, configuration, name, defaultDeliveryPolicy)
        {
            InBodiesMasterConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodiesMasterConnector));
            InBodiesSlaveConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodiesSlaveConnector));
            InCalibrationSlaveConnector = CreateInputConnectorFrom<Matrix<double>>(pipeline, nameof(InCalibrationSlaveConnector));
          
            var pair = InBodiesMasterConnector.Out.Pair(InBodiesSlaveConnector.Out);
            if (Configuration.WithVideoStream)
                pair.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>()).Do(Process);
            else
                pair.Do(Process);
        }

        protected void Process((List<SimplifiedBody>, List<SimplifiedBody>) data, Envelope envelope)
        {
            var (bodiesMaster, bodiesSlave) = data;
            lock (this)
            {
                Bitmap bitmap = new Bitmap(Configuration.Width, Configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb); ;
                Shared<Image> img = ImagePool.GetOrCreate(Configuration.Width, Configuration.Height, PixelFormat.BGRA_32bpp);
                Process(bodiesMaster, bodiesSlave, envelope, ref bitmap, ref img);
            }
        }

        protected void Process((List<SimplifiedBody>, List<SimplifiedBody>, Shared<Image>) data, Envelope envelope)
        {
            var (bodiesMaster, bodiesSlave, frame) = data;
            lock (this)
            {
                if (frame?.Resource != null)
                {
                    Bitmap bitmap = frame.Resource.ToBitmap();
                    Shared<Image> img = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    Process(bodiesMaster, bodiesSlave, envelope, ref bitmap, ref img);
                }
            }
        }

        protected void Process(List<SimplifiedBody> bodiesMaster, List<SimplifiedBody> bodiesSlave, Envelope envelope, ref Bitmap bitmap, ref Shared<Image> image)
        {
            Graphics graphics = Graphics.FromImage(bitmap);
            Font font = new Font(FontFamily.GenericSerif, 64);
            Lambda = DoNothing;
            ProcessBodies(ref graphics, bodiesMaster, new Pen(Color.LightGreen, LineThickness), new SolidBrush(Color.Green), font);
            Lambda = DoTransformation;
            ProcessBodies(ref graphics, bodiesSlave, new Pen(Color.LightSalmon, LineThickness), new SolidBrush(Color.Red), font);
            image.Resource.CopyFrom(bitmap);
            Out.Post(image, envelope.OriginatingTime);
            display.Update(image);
        }

        protected void ProcessBodies(ref Graphics graphics, List<SimplifiedBody> bodies, Pen linePen, SolidBrush color, Font font)
        {
            if (Lambda == null)
                return;
            foreach (var body in bodies)
            {
                foreach (var bone in AzureKinectBody.Bones)
                    DrawLine(ref graphics, linePen, Lambda(body.Joints[bone.ParentJoint]), Lambda(body.Joints[bone.ChildJoint]));

                MathNet.Spatial.Euclidean.Point2D head;
                if (toProjection(Lambda(body.Joints[JointId.Head]).Item2, out head))
                    graphics.DrawString(body.Id.ToString(), font, color, new PointF((float)head.X, (float)head.Y-150.0F));
            }
        }

        public Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> DoNothing(Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> vect)
        {
            return vect;
        }

        public Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> DoTransformation(Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> vect)
        {
            if(Calibration != null)
                return new Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D>(vect.Item1, Helpers.Helpers.CalculateTransform(vect.Item2, Calibration));
            return vect;
        }
    }
}

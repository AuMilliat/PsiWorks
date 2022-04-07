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
    public abstract class BodyCalibrationVisualizer : Visualizer.Visualizer
    {
        protected Connector<List<SimplifiedBody>> InBodiesMasterConnector;
        protected Connector<List<SimplifiedBody>> InBodiesSlaveConnector;
        protected Connector<Matrix<double>> InCalibrationSlaveConnector;

        public Receiver<List<SimplifiedBody>> InBodiesMaster => InBodiesMasterConnector.In;
        public Receiver<List<SimplifiedBody>> InBodiesSlave => InBodiesSlaveConnector.In;
        public Receiver<Matrix<double>> InCalibrationSlave => InCalibrationSlaveConnector.In;

        protected Matrix<double> slaveToMasterMatrix;

        protected delegate MathNet.Spatial.Euclidean.Vector3D DelegateThatShouldBeLambda(MathNet.Spatial.Euclidean.Vector3D vector);
        protected DelegateThatShouldBeLambda Lambda;
        public BodyCalibrationVisualizer(Pipeline pipeline, Matrix<double>? calibration) : base(pipeline)
        {
            slaveToMasterMatrix = calibration ?? Matrix<double>.Build.DenseIdentity(4, 4);
            InBodiesMasterConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodiesMasterConnector));
            InBodiesSlaveConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodiesSlaveConnector));
            InCalibrationSlaveConnector = CreateInputConnectorFrom<Matrix<double>>(pipeline, nameof(InCalibrationSlaveConnector));
          
            var pair = InBodiesMasterConnector.Out.Pair(InBodiesSlaveConnector.Out);
            pair.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>()).Do(Process);
        }
        protected void Process(ValueTuple<List<SimplifiedBody>, List<SimplifiedBody>, Shared<Image>> data, Envelope envelope)
        {
            if (Mute)
            {
                return;
            }
            var (bodiesMaster, bodiesSlave, frame) = data;
            lock (this)
            {
                //draw
                if (frame?.Resource != null)
                {
                    var bitmap = frame.Resource.ToBitmap();
                    Graphics graphics = Graphics.FromImage(bitmap);
                    Font font = new Font(FontFamily.GenericSerif, 64);
                    Lambda = DoNothing;
                    ProcessBodies(ref graphics, bodiesMaster, new Pen(Color.LightGreen, LineThickness), new SolidBrush(Color.Green), font);
                    Lambda = DoTransformation;
                    ProcessBodies(ref graphics, bodiesSlave, new Pen(Color.LightSalmon, LineThickness), new SolidBrush(Color.Red), font);
                    using var img = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    img.Resource.CopyFrom(bitmap);
                    Out.Post(img, envelope.OriginatingTime);
                    display.Update(img);
                }
            }
        }

        protected void ProcessBodies(ref Graphics graphics, List<SimplifiedBody> bodies, Pen linePen, SolidBrush color, Font font)
        {
            foreach (var body in bodies)
            {
                foreach (var bone in AzureKinectBody.Bones)
                {
                    DrawLine(ref graphics, linePen, Lambda(body.Joints[bone.ParentJoint].Item2), Lambda(body.Joints[bone.ChildJoint].Item2), color);
                }

                MathNet.Spatial.Euclidean.Point2D head = new MathNet.Spatial.Euclidean.Point2D();
                if (toProjection(Lambda(body.Joints[JointId.Head].Item2), out head))
                    graphics.DrawString(body.Id.ToString(), font, color, new PointF((float)head.X, (float)head.Y-150.0F));
            }
        }
        protected void DrawLine(ref Graphics graphics, Pen linePen, MathNet.Spatial.Euclidean.Vector3D joint1, MathNet.Spatial.Euclidean.Vector3D joint2, SolidBrush color)
        {
            MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
            MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
            if (toProjection(joint1, out p1) && toProjection(joint2, out p2))
            {
                var _p1 = new PointF((float)p1.X, (float)p1.Y);
                var _p2 = new PointF((float)p2.X, (float)p2.Y);
                graphics.DrawLine(linePen, _p1, _p2);
                graphics.FillEllipse(color, _p1.X, _p1.Y, circleRadius, circleRadius);
                graphics.FillEllipse(color, _p2.X, _p2.Y, circleRadius, circleRadius);
            }
        }
        public MathNet.Spatial.Euclidean.Vector3D DoNothing(MathNet.Spatial.Euclidean.Vector3D vect)
        {
            return vect;
        }

        public MathNet.Spatial.Euclidean.Vector3D DoTransformation(MathNet.Spatial.Euclidean.Vector3D vect)
        {
            return Helpers.Helpers.CalculateTransform(vect, slaveToMasterMatrix);
        }
    }
}

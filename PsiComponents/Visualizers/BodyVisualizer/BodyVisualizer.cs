using System.Drawing;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
using Helpers;
using Microsoft.Psi;
using Visualizer;

namespace BodyVisualizer
{
    public class BodyVisualizerConfguration
    {
        public bool WithVideoStream { get; set; } = true;
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
    }

    public abstract class BodyVisualizer : StreamVisualizer
    {
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;

        protected readonly string SkeletonsCountBase = "Skeletons: ";

        protected string skeletonCount = "";
        public string SkeletonCount
        {
            get => skeletonCount;
            set => SetProperty(ref skeletonCount, value);
        }

        protected BodyVisualizerConfguration Configuration;
        protected Dictionary<JointConfidenceLevel, SolidBrush> confidenceColor = new Dictionary<JointConfidenceLevel, SolidBrush>();

        public BodyVisualizer(Pipeline pipeline, BodyVisualizerConfguration? configuration) : base(pipeline)
        {
            Configuration = configuration ?? new BodyVisualizerConfguration();
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));

            confidenceColor.Add(JointConfidenceLevel.None, new SolidBrush(Color.Black));
            confidenceColor.Add(JointConfidenceLevel.Low, new SolidBrush(Color.Red));
            confidenceColor.Add(JointConfidenceLevel.Medium, new SolidBrush(Color.Yellow));
            confidenceColor.Add(JointConfidenceLevel.High, new SolidBrush(Color.Blue));
        }

        protected void Process(List<SimplifiedBody> data, Envelope envelope)
        {
            lock (this)
            {
                //draw
                Bitmap bitmap = new Bitmap(Configuration.Width, Configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Shared<Image> image = ImagePool.GetOrCreate(Configuration.Width, Configuration.Height, PixelFormat.BGRA_32bpp);
                Process(data, envelope, ref bitmap, ref image);
            }
        }

        protected void Process((List<SimplifiedBody>, Shared<Image>) data, Envelope envelope)
        {
            var (bodies, frame) = data;
            lock (this)
            {
                //draw
                if (frame?.Resource != null)
                {
                    Bitmap bitmap = frame.Resource.ToBitmap();
                    Shared<Image> image = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    Process(bodies, envelope, ref bitmap, ref image);
                }
            }
        }

        protected void Process(List<SimplifiedBody> data, Envelope envelope, ref Bitmap bitmap, ref Shared<Image> image)
        {
            using var graphics = Graphics.FromImage(bitmap);
            using var linePen = new Pen(Color.LightGreen, LineThickness);
            Font font = new Font(FontFamily.GenericSerif, 64);
            Brush brush = new SolidBrush(Color.Red);
            SkeletonCount = SkeletonsCountBase + data.Count.ToString();
            foreach (var body in data)
            {
                void drawLine(JointId joint1, JointId joint2)
                {
                    MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
                    MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
                    if (toProjection(body.Joints[joint1].Item2, out p1)
                        && toProjection(body.Joints[joint2].Item2, out p2))
                    {
                        var _p1 = new PointF((float)p1.X, (float)p1.Y);
                        var _p2 = new PointF((float)p2.X, (float)p2.Y);
                        graphics.DrawLine(linePen, _p1, _p2);
                        graphics.FillEllipse(confidenceColor[body.Joints[joint1].Item1], _p1.X, _p1.Y, circleRadius, circleRadius);
                        graphics.FillEllipse(confidenceColor[body.Joints[joint2].Item1], _p2.X, _p2.Y, circleRadius, circleRadius);
                    }
                }
                foreach (var bone in AzureKinectBody.Bones)
                    drawLine(bone.ParentJoint, bone.ChildJoint);
                MathNet.Spatial.Euclidean.Point2D head = new MathNet.Spatial.Euclidean.Point2D();
                if (toProjection(body.Joints[JointId.Head].Item2, out head))
                    graphics.DrawString(body.Id.ToString(), font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
            }
            image.Resource.CopyFrom(bitmap);
            Out.Post(image, envelope.OriginatingTime);
            display.Update(image);
        }
    }
}

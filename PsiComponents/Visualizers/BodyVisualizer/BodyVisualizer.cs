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

    public abstract class BodyVisualizer : BasicVisualizer
    {
        protected readonly string SkeletonsCountBase = "Skeletons: ";

        protected string skeletonCount = "";
        public string SkeletonCount
        {
            get => skeletonCount;
            set => SetProperty(ref skeletonCount, value);
        }

        public BodyVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(pipeline, configuration, name, defaultDeliveryPolicy)
        {}

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
            Graphics graphics = Graphics.FromImage(bitmap);
            Pen linePen = new Pen(Color.LightGreen, LineThickness);
            Font font = new Font(FontFamily.GenericSerif, 64);
            Brush brush = new SolidBrush(Color.Red);
            SkeletonCount = SkeletonsCountBase + data.Count.ToString();
            foreach (var body in data)
            {
                foreach (var bone in AzureKinectBody.Bones)
                    DrawLine(ref graphics, linePen, body.Joints[bone.ParentJoint], body.Joints[bone.ChildJoint]);

                MathNet.Spatial.Euclidean.Point2D head;
                if (toProjection(body.Joints[JointId.Head].Item2, out head))
                    graphics.DrawString(body.Id.ToString(), font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
            }
            image.Resource.CopyFrom(bitmap);
            Out.Post(image, envelope.OriginatingTime);
            display.Update(image);
        }
    }
}

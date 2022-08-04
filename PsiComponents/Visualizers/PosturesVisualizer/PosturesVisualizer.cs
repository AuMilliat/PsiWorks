using System.Drawing;
using Visualizer;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Helpers;
using Postures;

namespace PosturesVisualizer
{
    public abstract class PosturesVisualizer : BasicVisualizer
    {
        protected Connector<Dictionary<uint, List<Postures.Postures>>> InPosturesConnector;
        public Receiver<Dictionary<uint, List<Postures.Postures>>> InPostures => InPosturesConnector.In;

        public PosturesVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration) : base(pipeline, configuration)
        {
            InPosturesConnector = CreateInputConnectorFrom<Dictionary<uint, List<Postures.Postures>>>(pipeline, nameof(InPostures));
            var pair = InBodiesConnector.Join(InPosturesConnector);
            if (Configuration.WithVideoStream)
                pair.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>()).Do(Process);
            else
                pair.Do(Process);
        }

        protected void Process((List<SimplifiedBody>, Dictionary<uint, List<Postures.Postures>>) data, Envelope envelope)
        {
            var (bodies, postures) = data;
            lock (this)
            {
                Bitmap bitmap = new Bitmap(Configuration.Width, Configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Shared<Image> image = ImagePool.GetOrCreate(Configuration.Width, Configuration.Height, PixelFormat.BGRA_32bpp);
                Graphics graphics = Graphics.FromImage(bitmap);
                Process(ref graphics, bodies, postures, envelope, ref bitmap, ref image);
            }
        }

        protected void Process((List<SimplifiedBody>, Dictionary<uint, List<Postures.Postures>>, Shared<Image>) data, Envelope envelope)
        {
            var (bodies, postures, frame) = data;
            lock (this)
            {
                //draw
                if (frame?.Resource != null)
                {
                    Bitmap bitmap = frame.Resource.ToBitmap();
                    Graphics graphics = Graphics.FromImage(bitmap);
                    Shared<Image> image = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    Process(ref graphics, bodies, postures, envelope, ref bitmap, ref image);
                }
            }
        }

        protected void Process(ref Graphics graphics, List<SimplifiedBody> bodies, Dictionary<uint, List<Postures.Postures>> postures, Envelope envelope, ref Bitmap bitmap, ref Shared<Image> image)
        {
            Pen linePen = new Pen(Color.LightGreen, LineThickness);
            Font font = new Font(FontFamily.GenericSerif, 64);
            Brush brush = new SolidBrush(Color.Red);
            foreach (var body in bodies)
            {
                foreach (var bone in AzureKinectBody.Bones)
                    DrawLine(ref graphics, linePen, body.Joints[bone.ParentJoint], body.Joints[bone.ChildJoint]);

                MathNet.Spatial.Euclidean.Point2D head = new MathNet.Spatial.Euclidean.Point2D();
                if (toProjection(body.Joints[JointId.Head].Item2, out head))
                {
                    string headName = body.Id.ToString();
                    if (postures.ContainsKey(body.Id))
                        foreach(Postures.Postures posture in postures[body.Id])
                            headName += " - " +posture.ToString();
                    graphics.DrawString(headName, font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
                }
            }
            image.Resource.CopyFrom(bitmap);
            Out.Post(image, envelope.OriginatingTime);
            display.Update(image);
        }
    }
}

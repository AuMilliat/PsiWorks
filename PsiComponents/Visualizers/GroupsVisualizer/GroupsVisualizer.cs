using System.Drawing;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Visualizer;
using Helpers;

namespace GroupsVisualizer
{
    public abstract class GroupsVisualizer : BasicVisualizer
    {
        protected Connector<Dictionary<uint, List<uint>>> InGroupsConnector;
        public Receiver<Dictionary<uint, List<uint>>> InGroups => InGroupsConnector.In;

        public GroupsVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(pipeline, configuration, name, defaultDeliveryPolicy)
        {
            InGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(pipeline, nameof(InGroups));
            var pair = InBodiesConnector.Out.Join(InGroupsConnector.Out, Reproducible.Nearest<Dictionary<uint, List<uint>>>());
            if (Configuration.WithVideoStream)
                pair.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>()).Do(Process);
            else
                pair.Do(Process);
        }

        protected void Process((List<SimplifiedBody>, Dictionary<uint, List<uint>>) data, Envelope envelope)
        {
            var (bodies, groups) = data;
            lock (this)
            {
                Bitmap bitmap = new Bitmap(Configuration.Width, Configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Shared<Image> image = ImagePool.GetOrCreate(Configuration.Width, Configuration.Height, PixelFormat.BGRA_32bpp);
                Graphics graphics = Graphics.FromImage(bitmap);
                Process(ref graphics, bodies, groups, envelope, ref bitmap, ref image);
            }
        }

        protected void Process((List<SimplifiedBody>, Dictionary<uint, List<uint>>, Shared<Image>) data, Envelope envelope)
        {
            var (bodies, groups, frame) = data;
            lock (this)
            {
                if (frame?.Resource != null)
                {
                    Bitmap bitmap = frame.Resource.ToBitmap();
                    Graphics graphics = Graphics.FromImage(bitmap);
                    Shared<Image> image = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    Process(ref graphics, bodies, groups, envelope, ref bitmap, ref image);
                }
            }
        }

        protected void Process(ref Graphics graphics, List<SimplifiedBody> bodies, Dictionary<uint, List<uint>> groups, Envelope envelope, ref Bitmap bitmap, ref Shared<Image> image)
        {
            Dictionary<uint, SimplifiedBody> bodiesDics = new Dictionary<uint, SimplifiedBody>();
            foreach (SimplifiedBody body in bodies)
                bodiesDics[body.Id] = body;
            foreach (var group in groups)
            {
                Color groupColor = GenerateColorFromGroupId(group.Key);
                Brush brush = new SolidBrush(groupColor);
                Pen linePen = new Pen(groupColor, LineThickness);
                Font font = new Font(FontFamily.GenericSerif, 64);
                foreach (uint id in group.Value)
                {
                    if (!bodiesDics.ContainsKey(id))
                        continue;

                    SimplifiedBody body = bodiesDics[id];
                    foreach (var bone in AzureKinectBody.Bones)
                        DrawLine(ref graphics, linePen, body.Joints[bone.ParentJoint], body.Joints[bone.ChildJoint]);

                    MathNet.Spatial.Euclidean.Point2D head;
                    if (toProjection(body.Joints[JointId.Head].Item2, out head))
                    {
                        string text = body.Id.ToString() + " _ " + group.Key.ToString();
                        graphics.DrawString(body.Id.ToString(), font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
                    }
                }
            }
            image.Resource.CopyFrom(bitmap);
            Out.Post(image, envelope.OriginatingTime);
            display.Update(image);
         }
        
        protected Color GenerateColorFromGroupId(uint id)
        {
            double value = (id % 255) * 10.0;
            return Helpers.Helpers.ColorFromHSV(80.0, 90.0, value);
        }
    }
}

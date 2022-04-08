using System.Drawing;
using Microsoft.Psi.Imaging;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Visualizer;
using Helpers;

namespace GroupsVisualizer
{
    public class GroupsVisualizerConfguration
    {
        public int Width { get; set; } = 1080;
        public int Height { get; set; } = 920;
    }
    public abstract class GroupsVisualizer : BasicVisualizer
    {

        protected Connector<Dictionary<uint, List<uint>>> InGroupsConnector;
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;
        public Receiver<Dictionary<uint, List<uint>>> InGroups => InGroupsConnector.In;

        protected GroupsVisualizerConfguration Configuration;
        public GroupsVisualizer(Pipeline pipeline, GroupsVisualizerConfguration? configuration) : base(pipeline)
        {
            Configuration = configuration ?? new GroupsVisualizerConfguration();
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));
            InGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(pipeline, nameof(InGroups));

            InBodiesConnector.Out.Join(InGroupsConnector.Out, Reproducible.Nearest<Dictionary<uint, List<uint>>>()).Do(Process);
            Mute = true;
        }
        protected void Process((List<SimplifiedBody>, Dictionary<uint, List<uint>>) data, Envelope envelope)
        {
            var (bodies, groups) = data;
            Dictionary<uint, SimplifiedBody> bodiesDics = new Dictionary<uint, SimplifiedBody>();
            foreach(SimplifiedBody body in bodies)
                bodiesDics[body.Id] = body;
            lock (this)
            {
                Bitmap bitmap = new Bitmap(Configuration.Width, Configuration.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
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
                        void drawLine(JointId joint1, JointId joint2)
                        {
                            MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
                            MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
                            if (toProjection(body.Joints[joint1].Item2, out p1) && toProjection(body.Joints[joint2].Item2, out p2))
                            {
                                var _p1 = new PointF((float)p1.X, (float)p1.Y);
                                var _p2 = new PointF((float)p2.X, (float)p2.Y);
                                graphics.DrawLine(linePen, _p1, _p2);
                                graphics.FillEllipse(brush, _p1.X, _p1.Y, circleRadius, circleRadius);
                                graphics.FillEllipse(brush, _p2.X, _p2.Y, circleRadius, circleRadius);
                            }
                        }
                        foreach (var bone in AzureKinectBody.Bones)
                            drawLine(bone.ParentJoint, bone.ChildJoint);
                        MathNet.Spatial.Euclidean.Point2D head = new MathNet.Spatial.Euclidean.Point2D();
                        if (toProjection(body.Joints[JointId.Head].Item2, out head))
                        {
                            string text = body.Id.ToString() + " _ " + group.Key.ToString();
                            graphics.DrawString(body.Id.ToString(), font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
                        }
                    }
                }
                using var img = ImagePool.GetOrCreate(Configuration.Width, Configuration.Height, PixelFormat.BGRA_32bpp);
                img.Resource.CopyFrom(bitmap);
                Out.Post(img, envelope.OriginatingTime);
                display.Update(img);
            }
        }

        protected Color GenerateColorFromGroupId(uint id)
        {
            double value = (id % 255) * 10.0;
            return Helpers.Helpers.ColorFromHSV(80.0, 90.0, value);
        }
    }
}

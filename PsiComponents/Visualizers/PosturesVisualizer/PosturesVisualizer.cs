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
    public abstract class PosturesVisualizer : StreamVisualizer
    {
        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;

        protected Connector<Dictionary<uint, List<Postures.Postures>>> InPosturesConnector;
        public Receiver<Dictionary<uint, List<Postures.Postures>>> InPostures => InPosturesConnector.In;

        protected Dictionary<JointConfidenceLevel, SolidBrush> confidenceColor = new Dictionary<JointConfidenceLevel, SolidBrush>();
        public PosturesVisualizer(Pipeline pipeline) : base(pipeline)
        {
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));
            InPosturesConnector = CreateInputConnectorFrom<Dictionary<uint, List<Postures.Postures>>>(pipeline, nameof(InPostures));

            InBodiesConnector.Join(InPosturesConnector).Join(InColorImageConnector).Do(Process);

            confidenceColor.Add(JointConfidenceLevel.None, new SolidBrush(Color.Black));
            confidenceColor.Add(JointConfidenceLevel.Low, new SolidBrush(Color.Red));
            confidenceColor.Add(JointConfidenceLevel.Medium, new SolidBrush(Color.Yellow));
            confidenceColor.Add(JointConfidenceLevel.High, new SolidBrush(Color.Blue));


        }
        protected void Process((List<SimplifiedBody>,Dictionary<uint, List<Postures.Postures>>, Shared<Image>) data, Envelope envelope)
        {
            var (bodies, postures, frame) = data;
            lock (this)
            {
                //draw
                if (frame?.Resource != null)
                {
                    var bitmap = frame.Resource.ToBitmap();
                    using var linePen = new Pen(Color.LightGreen, LineThickness);
                    using var graphics = Graphics.FromImage(bitmap);
                    Font font = new Font(FontFamily.GenericSerif, 64);
                    Brush brush = new SolidBrush(Color.Red);
                    foreach (var body in bodies)
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
                        {
                            string headName = body.Id.ToString();
                            if (postures.ContainsKey(body.Id))
                                foreach(Postures.Postures posture in postures[body.Id])
                                    headName += " - " +posture.ToString();
                            graphics.DrawString(headName, font, brush, new PointF((float)head.X, (float)head.Y - 150.0f));
                        }
                    }
                    using var img = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    img.Resource.CopyFrom(bitmap);
                    Out.Post(img, envelope.OriginatingTime);
                    display.Update(img);
                }
            }
        }
    }
}

using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Helpers;

namespace AzureKinectBodyTrackerVisualizer
{
    public class AzureKinectBodyTrackerVisualizer : Subpipeline, IProducer<Shared<Image>>, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private Connector<List<SimplifiedBody>> InBodiesConnector;

        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;

        private Connector<Shared<Image>> InColorImageConnector;

        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;

        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        public Receiver<Shared<Image>> InColorImage => InColorImageConnector.In;

        public Emitter<Shared<Image>> Out { get; private set; }

        private DisplayVideo display = new DisplayVideo();

        public WriteableBitmap Image
        {
            get => display.VideoImage;
        }

       private bool mute = false;

        public bool Mute
        {
            get => mute;
            set => SetProperty(ref mute, value);
        }

        private int circleRadius = 18;

        public int CircleRadius
        {
            get => circleRadius;
            set => SetProperty(ref circleRadius, value);
        }

        private int lineThickness = 12;

        public int LineThickness
        {
            get => lineThickness;
            set => SetProperty(ref lineThickness, value);
        }


        private readonly string SkeletonsCountBase = "Skeletons: ";

        private string skeletonCount = "";
        public string SkeletonCount
        {
            get => skeletonCount;
            set => SetProperty(ref skeletonCount, value);
        }

        private Dictionary<JointConfidenceLevel, SolidBrush> confidenceColor = new Dictionary<JointConfidenceLevel, SolidBrush>();
        public AzureKinectBodyTrackerVisualizer(Pipeline pipeline) : base(pipeline)
        {
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));
            InColorImageConnector = CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(InColorImage));
            Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(Out));

            var joined1 = InBodiesConnector.Out.Fuse(InCalibrationConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>());//Note: Calibration only given once, Join is not aplicable here
            var joined2 = joined1.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>());
            joined2.Do(Process);

            pipeline.PipelineCompleted += OnPipelineCompleted;

            confidenceColor.Add(JointConfidenceLevel.None, new SolidBrush(Color.Black));
            confidenceColor.Add(JointConfidenceLevel.Low, new SolidBrush(Color.Red));
            confidenceColor.Add(JointConfidenceLevel.Medium, new SolidBrush(Color.Yellow));
            confidenceColor.Add(JointConfidenceLevel.High, new SolidBrush(Color.Blue));

            display.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(display.VideoImage))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
                }
            };
        }
 
        private void Process(ValueTuple<List<SimplifiedBody>, IDepthDeviceCalibrationInfo, Shared<Image>> data, Envelope envelope)
        {
            if (Mute)
            {
                return;
            }
            var (bodies, calibration, frame) = data;
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
                    SkeletonCount = SkeletonsCountBase + bodies.Count.ToString();
                    foreach (var body in bodies)
                    {
                        void drawLine(JointId joint1, JointId joint2)
                        {
                            MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
                            MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
                            if (calibration.TryGetPixelPosition(body.Joints[joint1].Item2.ToPoint3D(), out p1) 
                                && calibration.TryGetPixelPosition(body.Joints[joint2].Item2.ToPoint3D(), out p2))
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
                        if (calibration.TryGetPixelPosition(body.Joints[JointId.Head].Item2.ToPoint3D(), out head))
                            graphics.DrawString(body.Id.ToString(), font, brush, new PointF((float)head.X, (float)head.Y-150.0f)); 
                    }
                    using var img = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    img.Resource.CopyFrom(bitmap);
                    Out.Post(img, envelope.OriginatingTime);
                    display.Update(img);
                }
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            display.Clear();
        }
    }
}

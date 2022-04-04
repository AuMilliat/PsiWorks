using System.ComponentModel;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using AzureKinectBodyTrackerVisualizer;

namespace AzureKinectBodyCalibrationVisualizer
{
    public class AzureKinectBodyCalibrationVisualizer : Subpipeline, IProducer<Shared<Image>>, INotifyPropertyChanged
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

        private Connector<List<AzureKinectBody>> InBodiesMasterConnector;
        private Connector<List<AzureKinectBody>> InBodiesSlaveConnector;
        private Connector<Shared<Image>> InColorImageConnector;
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationMasterConnector;
        private Connector<Matrix<double>> InCalibrationSlaveConnector;

        public Receiver<List<AzureKinectBody>> InBodiesMaster => InBodiesMasterConnector.In;
        public Receiver<List<AzureKinectBody>> InBodiesSlave => InBodiesSlaveConnector.In;
        public Receiver<Shared<Image>> InColorImage => InColorImageConnector.In;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibrationMaster => InCalibrationMasterConnector.In;
        public Receiver<Matrix<double>> InCalibrationSlave => InCalibrationSlaveConnector.In;

        public Emitter<Shared<Image>> Out { get; private set; }

        private DisplayVideo display = new DisplayVideo();

        public WriteableBitmap Image
        {
            get => display.VideoImage;
        }

       private bool mute = true;

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

        private Matrix<double> slaveToMasterMatrix = Matrix<double>.Build.DenseIdentity(4,4);
        private IDepthDeviceCalibrationInfo MasterCalibration = null;

        public AzureKinectBodyCalibrationVisualizer(Pipeline pipeline) : base(pipeline)
        {
            InBodiesMasterConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(pipeline, nameof(InBodiesMasterConnector));
            InBodiesSlaveConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(pipeline, nameof(InBodiesSlaveConnector));
            InColorImageConnector = CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(InColorImageConnector));
            InCalibrationMasterConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibrationMasterConnector));
            InCalibrationSlaveConnector = CreateInputConnectorFrom<Matrix<double>>(pipeline, nameof(InCalibrationSlaveConnector));
            Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(Out));

            var fuse = InCalibrationSlaveConnector.Out.Fuse(InCalibrationMasterConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>()).Do(Initialisation);
            var pair = InBodiesMasterConnector.Out.Pair(InBodiesSlaveConnector.Out);
            pair.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>()).Do(Process);

            pipeline.PipelineCompleted += OnPipelineCompleted;

            display.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(display.VideoImage))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
                }
            };
        }
        private void Initialisation(ValueTuple<Matrix<double>, IDepthDeviceCalibrationInfo> data, Envelope envelope)
        {
            slaveToMasterMatrix = data.Item1;
            MasterCalibration = data.Item2;
            mute = false;
        }
        private void Process(ValueTuple<List<AzureKinectBody>, List<AzureKinectBody>, Shared<Image>> data, Envelope envelope)
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
                    ProcessBodies(ref graphics, bodiesMaster, new Pen(Color.LightGreen, LineThickness), new SolidBrush(Color.Green));
                    ProcessBodiesToTransform(ref graphics, bodiesSlave, new Pen(Color.LightSalmon, LineThickness), new SolidBrush(Color.Red));
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
       
        private void ProcessBodies(ref Graphics graphics, List<AzureKinectBody> bodies, Pen linePen, SolidBrush color)
        {
            foreach (var body in bodies)
            {
                foreach (var bone in AzureKinectBody.Bones)
                {
                    DrawLine(ref graphics, linePen, body.Joints[bone.ParentJoint].Pose.Origin, body.Joints[bone.ChildJoint].Pose.Origin, color);
                }
            }
        }
        private void ProcessBodiesToTransform(ref Graphics graphics, List<AzureKinectBody> bodies, Pen linePen, SolidBrush color)
        {
            foreach (var body in bodies)
            {
                foreach (var bone in AzureKinectBody.Bones)
                {
                    DrawLine(ref graphics, linePen, Helpers.Helpers.CalculateTransform(body.Joints[bone.ParentJoint].Pose.Origin.ToVector3D(), slaveToMasterMatrix).ToPoint3D(), Helpers.Helpers.CalculateTransform(body.Joints[bone.ChildJoint].Pose.Origin.ToVector3D(), slaveToMasterMatrix).ToPoint3D(), color);
                }
            }
        }
        private void DrawLine(ref Graphics graphics, Pen linePen, MathNet.Spatial.Euclidean.Point3D joint1, MathNet.Spatial.Euclidean.Point3D joint2, SolidBrush color)
        {
            MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
            MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
            if (MasterCalibration.TryGetPixelPosition(joint1, out p1)
                && MasterCalibration.TryGetPixelPosition(joint2, out p2))
            {
                var _p1 = new PointF((float)p1.X, (float)p1.Y);
                var _p2 = new PointF((float)p2.X, (float)p2.Y);
                graphics.DrawLine(linePen, _p1, _p2);
                graphics.FillEllipse(color, _p1.X, _p1.Y, circleRadius, circleRadius);
                graphics.FillEllipse(color, _p2.X, _p2.Y, circleRadius, circleRadius);
            }
        }
    }
}

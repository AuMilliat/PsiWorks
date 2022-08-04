using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Drawing;
using Helpers;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Visualizer
{ 
    public class BasicVisualizerConfiguration
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public bool WithVideoStream { get; set; } = true;
    }

    public abstract class BasicVisualizer : Subpipeline, IProducer<Shared<Image>>, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged ;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        protected Connector<List<SimplifiedBody>> InBodiesConnector;
        public Receiver<List<SimplifiedBody>> InBodies => InBodiesConnector.In;

        protected Connector<Shared<Image>> InColorImageConnector;
        public Receiver<Shared<Image>> InColorImage => InColorImageConnector.In;

        protected Dictionary<JointConfidenceLevel, SolidBrush> confidenceColor = new Dictionary<JointConfidenceLevel, SolidBrush>();

        protected BasicVisualizerConfiguration Configuration;

        public Emitter<Shared<Image>> Out { get; protected set; }

        protected DisplayVideo display = new DisplayVideo();

        public WriteableBitmap? Image
        {
            get => display.VideoImage;
        }

        protected int circleRadius = 18;

        public int CircleRadius
        {
            get => circleRadius;
            set => SetProperty(ref circleRadius, value);
        }

        protected int lineThickness = 12;

        public int LineThickness
        {
            get => lineThickness;
            set => SetProperty(ref lineThickness, value);
        }

        public BasicVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration) : base(pipeline)
        {
            Configuration = configuration ?? new BasicVisualizerConfiguration();
            Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(Out));
            InBodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(pipeline, nameof(InBodies));
            InColorImageConnector = CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(InColorImage));

            confidenceColor.Add(JointConfidenceLevel.None, new SolidBrush(Color.Black));
            confidenceColor.Add(JointConfidenceLevel.Low, new SolidBrush(Color.Red));
            confidenceColor.Add(JointConfidenceLevel.Medium, new SolidBrush(Color.Yellow));
            confidenceColor.Add(JointConfidenceLevel.High, new SolidBrush(Color.Blue));
        
            pipeline.PipelineCompleted += OnPipelineCompleted;

            display.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(display.VideoImage))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
            };

        }

        protected abstract bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj);

        protected void DrawLine(ref Graphics graphics, Pen linePen, Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> joint1, Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> joint2)
        {
            MathNet.Spatial.Euclidean.Point2D p1;
            MathNet.Spatial.Euclidean.Point2D p2;
            if (toProjection(joint1.Item2, out p1) && toProjection(joint2.Item2, out p2))
            {
                var _p1 = new PointF((float)p1.X, (float)p1.Y);
                var _p2 = new PointF((float)p2.X, (float)p2.Y);
                graphics.DrawLine(linePen, _p1, _p2);
                graphics.FillEllipse(confidenceColor[joint1.Item1], _p1.X, _p1.Y, circleRadius, circleRadius);
                graphics.FillEllipse(confidenceColor[joint1.Item1], _p2.X, _p2.Y, circleRadius, circleRadius);
            }
        }

        protected void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            display.Clear();
        }
    }
}

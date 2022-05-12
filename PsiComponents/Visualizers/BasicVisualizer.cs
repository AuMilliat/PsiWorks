using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Psi;

namespace Visualizer
{
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

        public Emitter<Shared<Image>> Out { get; protected set; }

        protected DisplayVideo display = new DisplayVideo();

        public WriteableBitmap? Image
        {
            get => display.VideoImage;
        }

        protected bool mute = false;

        public bool Mute
        {
            get => mute;
            set => SetProperty(ref mute, value);
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

        public BasicVisualizer(Pipeline pipeline) : base(pipeline)
        {
            Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(Out));

            pipeline.PipelineCompleted += OnPipelineCompleted;

            display.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(display.VideoImage))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
            };
        }
        protected abstract bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj);
        protected void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            display.Clear();
        }
    }
}

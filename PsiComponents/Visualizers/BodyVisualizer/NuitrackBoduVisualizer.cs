using NuitrackComponent;
using Microsoft.Psi;
using Image = Microsoft.Psi.Imaging.Image;

using Visualizer;

namespace BodyVisualizer
{
    public class NuitrackBodyVisualizer : BodyVisualizer
    {
        private NuitrackSensor Sensor;
        public NuitrackBodyVisualizer(Pipeline pipeline, NuitrackSensor sensor, BasicVisualizerConfiguration? configuration) : base(pipeline, configuration)
        {
            Sensor = sensor;
            if(Configuration.WithVideoStream)
            {
                var joined2 = InBodiesConnector.Out.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>());
                joined2.Do(Process);
            }
            else
                InBodiesConnector.Do(Process);
        }
        protected override bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}

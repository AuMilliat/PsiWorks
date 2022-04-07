using NuitrackComponent;
using Microsoft.Psi;
using Image = Microsoft.Psi.Imaging.Image;

namespace BodyTrackerVisualizer
{
    public class NuitrackBodyTrackerVisualizer : BodyTrackerVisualizer
    {

        private NuitrackSensor Sensor;
        public NuitrackBodyTrackerVisualizer(Pipeline pipeline, NuitrackSensor sensor) : base(pipeline)
        {
            Sensor = sensor;
           
            var joined2 = InBodiesConnector.Out.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>());
            joined2.Do(Process);
        }
        protected override bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}

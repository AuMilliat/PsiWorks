using NuitrackComponent;
using Microsoft.Psi;

namespace BodyVisualizer
{
    public class NuitrackBodyVisualizer : BodyVisualizer
    {
        private NuitrackSensor Sensor;
        public NuitrackBodyVisualizer(Pipeline pipeline, NuitrackSensor sensor, BodyVisualizerConfguration? configuration) : base(pipeline, configuration)
        {
            Sensor = sensor;
            InBodiesConnector.Do(Process);
        }
        protected override bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}

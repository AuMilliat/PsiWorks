using Microsoft.Psi;
using Visualizer;
using NuitrackComponent;
using MathNet.Spatial.Euclidean;

namespace GroupsVisualizer
{
    public class NuitrackGroupsVisualizer : GroupsVisualizer
    {
        private NuitrackSensor Sensor;
        public NuitrackGroupsVisualizer(Pipeline pipeline, NuitrackSensor sensor, BasicVisualizerConfiguration? configuration) : base(pipeline, configuration)
        {
            Sensor = sensor;
        }
        protected override bool toProjection(Vector3D point, out Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}

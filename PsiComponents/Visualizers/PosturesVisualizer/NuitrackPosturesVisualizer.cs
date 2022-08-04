using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using NuitrackComponent;
using MathNet.Spatial.Euclidean;
using Visualizer;

namespace PosturesVisualizer
{
    public class NuitrackPosturesVisualizer : PosturesVisualizer
    {
        private NuitrackSensor Sensor;
        
        public NuitrackPosturesVisualizer(Pipeline pipeline, NuitrackSensor sensor, BasicVisualizerConfiguration? configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(pipeline, configuration, name, defaultDeliveryPolicy)
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

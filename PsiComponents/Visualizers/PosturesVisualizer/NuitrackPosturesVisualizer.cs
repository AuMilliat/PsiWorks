using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using NuitrackComponent;
using MathNet.Spatial.Euclidean;

namespace PosturesVisualizer
{
    public class NuitrackPosturesVisualizer : PosturesVisualizer
    {
        private NuitrackSensor Sensor;
        public NuitrackPosturesVisualizer(Pipeline pipeline, NuitrackSensor sensor) : base(pipeline)
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

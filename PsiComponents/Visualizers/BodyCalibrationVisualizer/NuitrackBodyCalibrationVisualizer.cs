using MathNet.Numerics.LinearAlgebra;
using Microsoft.Psi;
using NuitrackComponent;
using Visualizer;

namespace BodyCalibrationVisualizer
{
    public class NuitrackBodyCalibrationVisualizer : BodyCalibrationVisualizer
    {

        private NuitrackSensor Sensor;
        public NuitrackBodyCalibrationVisualizer(Pipeline pipeline, NuitrackSensor sensor, BasicVisualizerConfiguration? configuration) : base(pipeline, configuration)
        {
            Sensor = sensor;
            InCalibrationSlaveConnector.Out.Do(Initialisation);
        }
        private void Initialisation(Matrix<double> data, Envelope envelope)
        {
            Calibration = data;
        }
        protected override bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}
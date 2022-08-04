using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;
using Visualizer;

namespace GroupsVisualizer
{
    public class AzureKinectGroupsVisualizer : GroupsVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo? CalibrationInfo=null;
        public AzureKinectGroupsVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(pipeline, configuration, name, defaultDeliveryPolicy)
        {
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));
            InCalibrationConnector.Out.Do(Initialisation);
        }

        private void Initialisation(IDepthDeviceCalibrationInfo data, Envelope envelope)
        {
            CalibrationInfo = data;
        }

        protected override bool toProjection(Vector3D point, out Point2D proj)
        {
            if (CalibrationInfo == null)
            {
                proj = new Point2D(0, 0);
                return false;
            }
            return CalibrationInfo.TryGetPixelPosition(point.ToPoint3D(), out proj);
        }
    }
}

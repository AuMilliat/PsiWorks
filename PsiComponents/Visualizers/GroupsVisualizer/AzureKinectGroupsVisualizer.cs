using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace GroupsVisualizer
{
    public class AzureKinectGroupsVisualizer : GroupsVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo CalibrationInfo;
        public AzureKinectGroupsVisualizer(Pipeline pipeline, GroupsVisualizerConfguration? configuration) : base(pipeline, configuration)
        {
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));
            InCalibrationConnector.Out.Do(Initialisation);
        }

        private void Initialisation(IDepthDeviceCalibrationInfo data, Envelope envelope)
        {
            Mute = false;
            CalibrationInfo = data;
        }

        protected override bool toProjection(Vector3D point, out Point2D proj)
        {
            return CalibrationInfo.TryGetPixelPosition(point.ToPoint3D(), out proj);
        }
    }
}

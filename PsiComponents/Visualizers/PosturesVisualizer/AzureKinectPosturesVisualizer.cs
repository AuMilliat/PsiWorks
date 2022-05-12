using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace PosturesVisualizer
{
    public class AzureKinectPosturesVisualizer : PosturesVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo? CalibrationInfo;
        public AzureKinectPosturesVisualizer(Pipeline pipeline) : base(pipeline)
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
            if (CalibrationInfo == null)
            {
                proj = new Point2D(-1, -1);
                return false;
            }
            return CalibrationInfo.TryGetPixelPosition(point.ToPoint3D(), out proj);
        }
    }
}

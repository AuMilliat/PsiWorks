using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi.Calibration;
using Helpers;
using Microsoft.Psi;

namespace BodyVisualizer
{
    public class AzureKinectBodyVisualizer : BodyVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo? CalibrationInfo = null;
        public AzureKinectBodyVisualizer(Pipeline pipeline, BodyVisualizerConfguration? configuration) : base(pipeline, configuration)
        {
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));

            var joined1 = InBodiesConnector.Out.Fuse(InCalibrationConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>());//Note: Calibration only given once, Join is not aplicable here
            joined1.Do(Process);
        }

        private void Process(ValueTuple<List<SimplifiedBody>, IDepthDeviceCalibrationInfo> data, Envelope envelope)
        {
            if (Mute)
            {
                return;
            }
            var (bodies, calibration) = data;
            CalibrationInfo = calibration;
            Process(bodies, envelope);
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

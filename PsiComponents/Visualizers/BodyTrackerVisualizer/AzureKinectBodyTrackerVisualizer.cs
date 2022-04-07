using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using Image = Microsoft.Psi.Imaging.Image;
using Helpers;
using MathNet.Spatial.Euclidean;

namespace BodyTrackerVisualizer
{
    public class AzureKinectBodyTrackerVisualizer : BodyTrackerVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo CalibrationInfo;
        public AzureKinectBodyTrackerVisualizer(Pipeline pipeline) : base(pipeline)
        {
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));
            
            var joined1 = InBodiesConnector.Out.Fuse(InCalibrationConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>());//Note: Calibration only given once, Join is not aplicable here
            var joined2 = joined1.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>());
            joined2.Do(Process);
        }

        private void Process(ValueTuple<List<SimplifiedBody>, IDepthDeviceCalibrationInfo, Shared<Image>> data, Envelope envelope)
        {
            if (Mute)
            {
                return;
            }
            var (bodies, calibration, frame) = data;
            CalibrationInfo = calibration;
            Process((bodies, frame), envelope);
        }

        protected override bool toProjection(Vector3D point, out Point2D proj)
        {
            return CalibrationInfo.TryGetPixelPosition(point.ToPoint3D(),out proj);
        }
    }
}

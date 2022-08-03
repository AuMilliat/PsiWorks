using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi.Calibration;
using Image = Microsoft.Psi.Imaging.Image;
using Helpers;
using Visualizer;
using Microsoft.Psi;

namespace BodyVisualizer
{
    public class AzureKinectBodyVisualizer : BodyVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibration => InCalibrationConnector.In;

        private IDepthDeviceCalibrationInfo? CalibrationInfo = null;
        public AzureKinectBodyVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration) : base(pipeline, configuration)
        {
            InCalibrationConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibration));

            var joined1 = InBodiesConnector.Out.Fuse(InCalibrationConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>());//Note: Calibration only given once, Join is not aplicable here
            if (Configuration.WithVideoStream)
            {
                var joined2 = joined1.Join(InColorImageConnector.Out, Reproducible.Nearest<Shared<Image>>());
                joined2.Do(Process);
            }
            else
                joined1.Do(Process);
        }

        private void Process(ValueTuple<List<SimplifiedBody>, IDepthDeviceCalibrationInfo> data, Envelope envelope)
        {
            var (bodies, calibration) = data;
            CalibrationInfo = calibration;
            Process(bodies, envelope);
        }

        private void Process(ValueTuple<List<SimplifiedBody>, IDepthDeviceCalibrationInfo, Shared<Image>> data, Envelope envelope)
        {
            var (bodies, calibration, image) = data;
            CalibrationInfo = calibration;
            Process((bodies, image), envelope);
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

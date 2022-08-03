using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using Visualizer;

namespace BodyCalibrationVisualizer
{
    public class AzureKinectBodyCalibrationVisualizer : BodyCalibrationVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationMasterConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibrationMaster => InCalibrationMasterConnector.In;
       
        private IDepthDeviceCalibrationInfo? MasterCalibration = null;

        public AzureKinectBodyCalibrationVisualizer(Pipeline pipeline, BasicVisualizerConfiguration? configuration, bool calibrationByPipeline) : base(pipeline, configuration)
        {
            InCalibrationMasterConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibrationMasterConnector));
            
            if(calibrationByPipeline)
                InCalibrationSlaveConnector.Out.Fuse(InCalibrationMasterConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>()).Do(Initialisation);
            else
                InCalibrationMasterConnector.Out.Do(Initialisation);
        }
        private void Initialisation(ValueTuple<Matrix<double>, IDepthDeviceCalibrationInfo> data, Envelope envelope)
        {
            Calibration = data.Item1;
            MasterCalibration = data.Item2;
        }

        private void Initialisation(IDepthDeviceCalibrationInfo data, Envelope envelope)
        {
            MasterCalibration = data;
        }
        protected override bool toProjection(Vector3D point, out Point2D proj)
        {
            if (MasterCalibration == null)
            {
                proj = new Point2D(-1, -1);
                return false;
            }
            return MasterCalibration.TryGetPixelPosition(point.ToPoint3D(), out proj);
        }
    }
}

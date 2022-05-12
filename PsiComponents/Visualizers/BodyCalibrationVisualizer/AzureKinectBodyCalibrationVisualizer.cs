using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;

namespace BodyCalibrationVisualizer
{
    public class AzureKinectBodyCalibrationVisualizer : BodyCalibrationVisualizer
    {
        private Connector<IDepthDeviceCalibrationInfo> InCalibrationMasterConnector;
        public Receiver<IDepthDeviceCalibrationInfo> InCalibrationMaster => InCalibrationMasterConnector.In;
       
        private IDepthDeviceCalibrationInfo? MasterCalibration = null;

        public AzureKinectBodyCalibrationVisualizer(Pipeline pipeline, Matrix<double>? calibration) : base(pipeline, calibration)
        {
            InCalibrationMasterConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(InCalibrationMasterConnector));
            
            if(calibration == null)
                InCalibrationSlaveConnector.Out.Fuse(InCalibrationMasterConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>()).Do(Initialisation);
            else
                InCalibrationMasterConnector.Out.Do(Initialisation);
        }
        private void Initialisation(ValueTuple<Matrix<double>, IDepthDeviceCalibrationInfo> data, Envelope envelope)
        {
            slaveToMasterMatrix = data.Item1;
            MasterCalibration = data.Item2;
            mute = false;
        }

        private void Initialisation(IDepthDeviceCalibrationInfo data, Envelope envelope)
        {
            MasterCalibration = data;
            mute = false;
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

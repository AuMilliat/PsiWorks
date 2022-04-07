﻿using MathNet.Numerics.LinearAlgebra;
using Microsoft.Psi;
using NuitrackComponent;

namespace BodyCalibrationVisualizer
{
    public class NuitrackBodyCalibrationVisualizer : BodyCalibrationVisualizer
    {

        private NuitrackSensor Sensor;
        public NuitrackBodyCalibrationVisualizer(Pipeline pipeline, NuitrackSensor sensor, Matrix<double>? calibration) : base(pipeline, calibration)
        {
            Sensor = sensor;
            InCalibrationSlaveConnector.Out.Do(Initialisation);
        }
        private void Initialisation(Matrix<double> data, Envelope envelope)
        {
            slaveToMasterMatrix = data;
            mute = false;
        }
        protected override bool toProjection(MathNet.Spatial.Euclidean.Vector3D point, out MathNet.Spatial.Euclidean.Point2D proj)
        {
            proj = Sensor.getProjCoordinates(point);
            return Helpers.Helpers.IsValidPoint2D(proj);
        }
    }
}
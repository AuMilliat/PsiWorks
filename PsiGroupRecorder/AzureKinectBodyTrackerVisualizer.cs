﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Calibration;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Image = Microsoft.Psi.Imaging.Image;

namespace PsiGroupRecorder
{
    public class AzureKinectBodyTrackerVisualizer : Subpipeline, IProducer<Shared<Image>>, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private Connector<List<AzureKinectBody>> BodiesInConnector;

        private Connector<IDepthDeviceCalibrationInfo> CalibrationInConnector;

        private Connector<Shared<Image>> ColorImageInConnector;

        public Receiver<List<AzureKinectBody>> BodiesIn => BodiesInConnector.In;

        public Receiver<IDepthDeviceCalibrationInfo> CalibrationIn => CalibrationInConnector.In;

        public Receiver<Shared<Image>> ColorImageIn => ColorImageInConnector.In;

        public Emitter<Shared<Image>> Out { get; private set; }

        private DisplayVideo display = new DisplayVideo();

        public WriteableBitmap Image
        {
            get => display.VideoImage;
        }

       private bool mute = false;

        public bool Mute
        {
            get => mute;
            set => SetProperty(ref mute, value);
        }

        private int circleRadius = 12;

        public int CircleRadius
        {
            get => circleRadius;
            set => SetProperty(ref circleRadius, value);
        }

        private int lineThickness = 8;

        public int LineThickness
        {
            get => lineThickness;
            set => SetProperty(ref lineThickness, value);
        }


        private readonly string SkeletonsCountBase = "Skeletons: ";

        private string skeletonCount = "";
        public string SkeletonCount
        {
            get => skeletonCount;
            set => SetProperty(ref skeletonCount, value);
        }

        public AzureKinectBodyTrackerVisualizer(Pipeline pipeline) : base(pipeline)
        {
            BodiesInConnector = CreateInputConnectorFrom<List<AzureKinectBody>>(pipeline, nameof(BodiesIn));
            CalibrationInConnector = CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(pipeline, nameof(CalibrationIn));
            ColorImageInConnector = CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(ColorImageIn));
            Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(Out));

            var joined1 = BodiesInConnector.Out.Fuse(CalibrationInConnector.Out, Available.Nearest<IDepthDeviceCalibrationInfo>());//Note: Calibration only given once, Join is not aplicable here
            var joined2 = joined1.Join(ColorImageInConnector.Out, Reproducible.Nearest<Shared<Image>>());
            joined2.Do(Process);

            pipeline.PipelineCompleted += OnPipelineCompleted;

            display.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(display.VideoImage))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
                }
            };
        }
 
        private void Process(ValueTuple<List<AzureKinectBody>, IDepthDeviceCalibrationInfo, Shared<Image>> data, Envelope envelope)
        {
            if (Mute)
            {
                return;
            }
            var (bodies, calibration, frame) = data;
            lock (this)
            {
                //draw
                if (frame?.Resource != null)
                {
                    var bitmap = frame.Resource.ToBitmap();
                    using var linePen = new Pen(Color.LightGreen, LineThickness);
                    using var circleBrush = new SolidBrush(Color.LightGreen);
                    using var graphics = Graphics.FromImage(bitmap);
                    SkeletonCount = SkeletonsCountBase + bodies.Count.ToString();
                    foreach (var body in bodies)
                    {
                        void drawLine(JointId joint1, JointId joint2)
                        {
                            MathNet.Spatial.Euclidean.Point2D p1 = new MathNet.Spatial.Euclidean.Point2D();
                            MathNet.Spatial.Euclidean.Point2D p2 = new MathNet.Spatial.Euclidean.Point2D();
                            if (calibration.TryGetPixelPosition(body.Joints[joint1].Pose.Origin, out p1) 
                                && calibration.TryGetPixelPosition(body.Joints[joint2].Pose.Origin, out p2))
                            {
                                if (IsValidPoint2D(p1) && IsValidPoint2D(p2))
                                {
                                    var _p1 = new PointF((float)p1.X, (float)p1.Y);
                                    var _p2 = new PointF((float)p2.X, (float)p2.Y);
                                    graphics.DrawLine(linePen, _p1, _p2);
                                    graphics.FillEllipse(circleBrush, _p1.X, _p1.Y, circleRadius, circleRadius);
                                    graphics.FillEllipse(circleBrush, _p2.X, _p2.Y, circleRadius, circleRadius);
                                }
                            }
                        }
                        foreach (var bone in AzureKinectBody.Bones)
                        {
                            drawLine(bone.ParentJoint, bone.ChildJoint);
                        }
                    }
                    using var img = ImagePool.GetOrCreate(frame.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
                    img.Resource.CopyFrom(bitmap);
                    Out.Post(img, envelope.OriginatingTime);
                    display.Update(img);
                }
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            display.Clear();
        }

        private static bool IsValidDouble(double val)
        {
            if (Double.IsNaN(val))
            {
                return false;
            }
            if (Double.IsInfinity(val))
            {
                return false;
            }
            return true;
        }

        private static bool IsValidPoint2D(MathNet.Spatial.Euclidean.Point2D point) => IsValidDouble(point.X) && IsValidDouble(point.Y);
    }
}

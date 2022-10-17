﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using OpenFaceInterop;

namespace OpenSense.Component.OpenFace {
    public sealed class OpenFace : IConsumer<Shared<Image>>, IProducer<PoseAndEyeAndFace>, INotifyPropertyChanged {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (!EqualityComparer<T>.Default.Equals(field, value)) {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private CLNF landmarkDetector;
        private FaceDetector faceDetector;
        private FaceAnalyser faceAnalyser;
        private GazeAnalyser gazeAnalyser;
        private FaceModelParameters faceModelParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFace"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public OpenFace(Pipeline pipeline) {
            // Image receiver.
            In = pipeline.CreateReceiver<Shared<Image>>(this, ReceiveImage, nameof(In));

            // Pose data emitter.
            PoseOut = pipeline.CreateEmitter<Pose>(this, nameof(PoseOut));

            // Gaze data emitter.
            EyeOut = pipeline.CreateEmitter<Eye>(this, nameof(EyeOut));

            FaceOut = pipeline.CreateEmitter<Face>(this, nameof(FaceOut));

            Out = pipeline.CreateEmitter<PoseAndEyeAndFace>(this, nameof(Out));

            pipeline.PipelineRun += Initialize;
            pipeline.PipelineCompleted += OnPipelineCompleted;
        }

        private void Initialize(object sender, PipelineRunEventArgs e) {

            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            faceModelParameters = new FaceModelParameters(rootDirectory, true, false, false);
            faceModelParameters.optimiseForVideo();

            faceDetector = new FaceDetector(faceModelParameters.GetHaarLocation(), faceModelParameters.GetMTCNNLocation());
            if (!faceDetector.IsMTCNNLoaded()) {
                faceModelParameters.SetFaceDetector(false, true, false);
            }

            landmarkDetector = new CLNF(faceModelParameters);
            faceAnalyser = new FaceAnalyser(rootDirectory, dynamic: true, output_width: 112, mask_aligned: true);
            gazeAnalyser = new GazeAnalyser();

            landmarkDetector.Reset();
            faceAnalyser.Reset();
        }

        public ILogger Logger { private get; set; }

        private bool mute = false;

        public bool Mute {
            get => mute;
            set => SetProperty(ref mute, value);
        }

        private float cameraCalibFx = 500;

        public float CameraCalibFx {
            get => cameraCalibFx;
            set => SetProperty(ref cameraCalibFx, value);
        }

        private float cameraCalibFy = 500;

        public float CameraCalibFy {
            get => cameraCalibFy;
            set => SetProperty(ref cameraCalibFy, value);
        }

        private float cameraCalibCx = 640 / 2f;

        public float CameraCalibCx {
            get => cameraCalibCx;
            set => SetProperty(ref cameraCalibCx, value);
        }

        private float cameraCalibCy = 480 / 2f;

        public float CameraCalibCy {
            get => cameraCalibCy;
            set => SetProperty(ref cameraCalibCy, value);
        }

        private IDataWriter<PoseAndEyeAndFace> dataWriter;

        public IDataWriter<PoseAndEyeAndFace> DataWriter {
            get => dataWriter;
            set => SetProperty(ref dataWriter, value);
        }

        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Image>> In { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the pose data output stream.
        /// </summary>
        public Emitter<Pose> PoseOut { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the gaze data output stream.
        /// </summary>
        public Emitter<Eye> EyeOut { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the face data output stream.
        /// </summary>
        public Emitter<Face> FaceOut { get; private set; }

        public Emitter<PoseAndEyeAndFace> Out { get; private set; }

        /// <summary>
        /// The receive method for the ImageIn receiver.
        /// This executes every time a message arrives on ImageIn.
        /// </summary>
        private void ReceiveImage(Shared<Image> input, Envelope envelope) {
            if (Mute) {
                return;
            }
            try {
                var width = input.Resource.Width;
                var height = input.Resource.Height;
                static Vector2 pointToVector2(Point p) {
                    return new Vector2((float)p.X, (float)p.Y);
                }
                static Vector2 tupleToVector2(Tuple<float, float> tuple) {
                    return new Vector2(tuple.Item1, tuple.Item2);
                }
                using (var colorSharedImage = ImagePool.GetOrCreate(width, height, input.Resource.PixelFormat))
                using (var graySharedImage = ImagePool.GetOrCreate(width, height, PixelFormat.Gray_8bpp)) {
                    input.Resource.CopyTo(colorSharedImage.Resource);
                    var colorImageBuffer = new ImageBuffer(width, height, colorSharedImage.Resource.ImageData, colorSharedImage.Resource.Stride);
                    var grayImageBuffer = new ImageBuffer(width, height, graySharedImage.Resource.ImageData, graySharedImage.Resource.Stride);
                    Methods.ToGray(colorImageBuffer, grayImageBuffer);
                    using (var colorRawImage = Methods.ToRaw(colorImageBuffer))
                    using (var grayRawImage = Methods.ToRaw(grayImageBuffer)) {
                        if (landmarkDetector.DetectLandmarksInVideo(colorRawImage, faceModelParameters, grayRawImage)) {

                            var rawAllLandmarks = landmarkDetector.CalculateAllLandmarks();

                            //double confidence = landmarkDetector.GetConfidence();
                            //float scale = landmarkDetector.GetRigidParams()[0];

                            //List<float> nonRigidParameters = landmarkDetector.GetNonRigidParams();
                            //List<bool> visibilities = landmarkDetector.GetVisibilities();
                            //faceAnalyser.AddNextFrame(colorRawImage, landmarkDetector.CalculateAllLandmarks(), detected, true);
                            //Dictionary<string, double> aus = faceAnalyser.GetCurrentAUsReg();

                            // Pose.
                            var allLandmarks = rawAllLandmarks.Select(tupleToVector2);
                            var visiableLandmarks = landmarkDetector
                                .CalculateVisibleLandmarks()
                                .Select(tupleToVector2);
                            var landmarks3D = landmarkDetector
                                .Calculate3DLandmarks(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy)
                                .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                            var poseData = new List<float>();
                            landmarkDetector.GetPose(poseData, CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var box = landmarkDetector.CalculateBox(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var boxConverted = box.Select(line => {
                                var a = pointToVector2(line.Item1);
                                var b = pointToVector2(line.Item2);
                                return (a, b);
                            });
                            var headPose = new Pose(poseData, allLandmarks, visiableLandmarks, landmarks3D, boxConverted);
                            PoseOut.Post(headPose, envelope.OriginatingTime);

                            // Gaze.
                            gazeAnalyser.AddNextFrame(landmarkDetector, success: true, CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var eyeLandmarks = landmarkDetector
                                .CalculateAllEyeLandmarks()
                                .Select(tupleToVector2);
                            var visiableEyeLandmarks = landmarkDetector
                                .CalculateVisibleEyeLandmarks()
                                .Select(tupleToVector2);
                            var eyeLandmarks3D = landmarkDetector
                                .CalculateAllEyeLandmarks3D(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy)
                                .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                            var (leftPupil, rightPupil) = gazeAnalyser.GetGazeCamera();
                            var (angleX, angleY) = gazeAnalyser.GetGazeAngle();//Not accurate
                            var gazeLines = gazeAnalyser.CalculateGazeLines(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var gazeLinesConverted = gazeLines.Select(line => {
                                var a = pointToVector2(line.Item1);
                                var b = pointToVector2(line.Item2);
                                return (a, b);
                            });
                            var gaze = new Eye(
                                    new GazeVector(
                                            new Vector3(leftPupil.Item1, leftPupil.Item2, leftPupil.Item3),
                                            new Vector3(rightPupil.Item1, rightPupil.Item2, rightPupil.Item3)
                                        ),
                                    new Vector2(angleX, angleY),
                                    eyeLandmarks,
                                    visiableEyeLandmarks,
                                    eyeLandmarks3D,
                                    gazeLinesConverted
                                );
                            EyeOut.Post(gaze, envelope.OriginatingTime);

                            //Face
                            var (actionUnitIntensities, actionUnitOccurences) = faceAnalyser.PredictStaticAUsAndComputeFeatures(colorRawImage, rawAllLandmarks);//image mode, so not using faceAnalyser.AddNextFrame()
                            var actionUnits = actionUnitIntensities.ToDictionary(
                                kv => kv.Key.Substring(2)/*remove prefix "AU"*/.TrimStart('0'), 
                                kv => new ActionUnit(intensity: kv.Value, presence: actionUnitOccurences[kv.Key])
                            );
                            var face = new Face(actionUnits);
                            FaceOut.Post(face, envelope.OriginatingTime);

                            //All
                            var headPoseAndGaze = new PoseAndEyeAndFace(headPose.DeepClone(), gaze.DeepClone(), face.DeepClone());
                            DataWriter?.Write(headPoseAndGaze, envelope);
                            Out.Post(headPoseAndGaze, envelope.OriginatingTime);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger?.LogError(ex, "OpenFace exception");
                Mute = true;
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e) {
        }
    }
}

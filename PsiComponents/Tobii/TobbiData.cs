using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tobii.Research;
using Microsoft.Psi;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Psi.Imaging;
using Helpers;

namespace Tobii
{
    public class TobbiGazeData
    {
        // Gets or sets the gaze data for the left eye.
        public EyeData LeftEye { get; set; }

        // Gets or sets the gaze data for the right eye.
        public EyeData RightEye { get; set; }

        // Contructor from device event.
        public TobbiGazeData(GazeDataEventArgs data)
        {
            LeftEye = data.LeftEye;
            RightEye = data.RightEye;
        }
    }

    public class TobiiEyeOpennessData
    {
        // Gets or sets  the validity for the left eye.
        public Validity LeftEyeValidity { get; set; }

        // Gets or sets  the validity for the right eye.
        public Validity RightEyeValidity { get; set; }

        // Gets or sets  the value for the left eye in mm.
        public float LeftEyeValue { get; set; }

        // Gets or sets  the value for the right eye in mm.
        public float RightEyeValue { get; set; }

        // Contructor from device event.
        public TobiiEyeOpennessData(EyeOpennessDataEventArgs data)
        {
            LeftEyeValidity = data.LeftEyeValidity;
            RightEyeValidity = data.RightEyeValidity;
            LeftEyeValue = data.LeftEyeValue;
            RightEyeValue = data.RightEyeValue;
        }
    }

    public class TobiiUserPositionGuide
    {
        // Gets or sets the user position guide for the left eye.
        public UserPositionGuide LeftEye { get; set; }

        // Gets or sets the user position guide for the right eye.
        public UserPositionGuide RightEye { get; set; }

        // Contructor from device event.
        public TobiiUserPositionGuide(UserPositionGuideEventArgs data)
        {
            LeftEye = data.LeftEye;
            RightEye = data.RightEye;
        }
    }

    public class TobiiHMDGazeData
    {
        // Gets or sets the HMD gaze data for the left eye.
        public HMDEyeData LeftEye { get; set; }

        // Gets or sets the HMD gaze data for the right eye.
        public HMDEyeData RightEye { get; set; }

        // Contructor from device event.
        public TobiiHMDGazeData(HMDGazeDataEventArgs data)
        {
            LeftEye = data.LeftEye;
            RightEye = data.RightEye;
        }
    }

    public class TobiiTimeSynchronizationReference
    {
        // Gets or sets the time stamp in microseconds when the computer sent the request to the eye tracker.
        public long SystemRequestTimeStamp { get; set; }

        // Gets or sets the time stamp in microseconds when the eye tracker received the request, according to the eye trackers clock.
        public long DeviceTimeStamp { get; set; }

        // Gets the time stamp in microseconds when the computer received the response from the eye tracker.
        public long SystemResponseTimeStamp { get; set; }

        // Contructor from device event.
        public TobiiTimeSynchronizationReference(TimeSynchronizationReferenceEventArgs data)
        {
            SystemRequestTimeStamp = data.SystemRequestTimeStamp;
            DeviceTimeStamp = data.DeviceTimeStamp;
            SystemResponseTimeStamp = data.SystemResponseTimeStamp;
        }
    }

    public class TobiiExternalSignal
    {
        // Gets or sets the value of the external signal port on the eye tracker.
        public long Value { get; set; }

        // Gets or sets the type of value change.
        public ExternalSignalChangeType ChangeType { get; set; }

        // Contructor from device event.
        public TobiiExternalSignal(ExternalSignalValueEventArgs data)
        {
            Value = data.Value;
            ChangeType = data.ChangeType;
        }
    }

    public class TobiiError
    {
        // Gets or sets the log source.
        public EventErrorType ErrorType { get; set; }

        // Gets or sets the log level.
        public EventErrorSource Source { get; set; }

        // Gets or sets the log message.
        public string Message { get; set; }

        // Contructor from device event.
        public TobiiError(EventErrorEventArgs data)
        {
            ErrorType = data.ErrorType;
            Source = data.Source;
            Message = data.Message;
        }
    }

    public class TobiiEyeImage
    {
        // Gets or sets the type of eye image.
        public EyeImageType ImageType { get; set; }

        // Gets or sets the region ID of the eye image.
        public int RegionId { get; set; }

        // Gets or set the top position in pixels of eye image.
        public int Top { get; set; }

        // Gets or sets the left position in pixels of eye image.
        public int Left { get; set; }

        // Gets or sets which camera generated the image.
        public int CameraId { get; set; }

        // Gets or sets the bitmap data sent by the eye tracker, that can be converted to several image formats.
        public Shared<Image> Image { get; set; }

        // Contructor from device event.
        public TobiiEyeImage(EyeImageEventArgs data)
        {
            ImageType = data.ImageType;
            RegionId = data.RegionId;
            Top = data.Top;
            Left = data.Left;
            CameraId = data.CameraId;
            //ToDo test and do correct stuff
            //Image = ImagePool.GetOrCreate(.Resource.Width, frame.Resource.Height, frame.Resource.PixelFormat);
            Image = ImagePool.GetOrCreate(1, 1, PixelFormat.Gray_8bpp);
        }
    }

    public class TobiiEyeImageRaw
    {
        // Gets or sets the type of eye image.
        public EyeImageType ImageType { get; set; }

        // Gets or sets the region ID of the eye image.
        public int RegionId { get; set; }

        // Gets or set the top position in pixels of eye image.
        public int Top { get; set; }

        // Gets or sets the left position in pixels of eye image.
        public int Left { get; set; }

        // Gets or sets which camera generated the image.
        public int CameraId { get; set; }

        // Gets or sets the bitmap data sent by the eye tracker, that can be converted to several image formats.
        public Shared<Image> Image { get; set; }

        // Contructor from device event.
        public TobiiEyeImageRaw(EyeImageRawEventArgs data)
        {
            ImageType = data.ImageType;
            RegionId = data.RegionId;
            Top = data.Top;
            Left = data.Left;
            CameraId = data.CameraId;
            Image = ImagePool.GetOrCreate(data.Width, data.Height, Helpers.Helpers.BitToPixelFormat(data.BitsPerPixel));
            Image.Resource.CopyFrom(data.ImageData);
        }
    }
}

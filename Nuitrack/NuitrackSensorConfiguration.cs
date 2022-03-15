﻿using Microsoft.Psi;

namespace NuitrackComponent
{
    public class NuitrackCoreConfiguration
    {
        /// <summary>
        /// Gets or sets the index of the device to open.
        /// </summary>
        public int DeviceIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the nuitrack licence key of the device to open.
        /// </summary>
        public string ActivationKey { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the color stream is emitted.
        /// </summary>
        public bool OutputColor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the depth stream is emitted.
        /// </summary>
        public bool OutputDepth { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputSkeletonTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputHandTracking { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputUserTracking { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputGestureRecognizer { get; set; } = false;
    }
}

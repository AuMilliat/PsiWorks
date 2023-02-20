using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.Research;

namespace Tobii
{
    public class TobiiCoreConfiguration
    {
        /// <summary>
        /// Gets or sets the index of the device to open.
        /// </summary>
        public int DeviceIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the device type to open.
        /// </summary>
        public bool IsHMD { get; set; } = false;

        /// <summary>
        /// Gets or sets the device calibration.
        /// </summary>
        public CalibrationData Calibration { get; set; } = new CalibrationData(null);

        /// <summary>
        /// Gets or sets the features licences.
        /// </summary>
        public LicenseCollection Licenses { get; set; } = new LicenseCollection(new List<LicenseKey>());
    }
}

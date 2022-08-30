using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tobii
{
    public class TobiiCoreConfiguration
    {
        /// <summary>
        /// Gets or sets the index of the device to open.
        /// </summary>
        public int DeviceIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the device tyoe to open.
        /// </summary>
        public bool IsHMD { get; set; } = false;
    }
}

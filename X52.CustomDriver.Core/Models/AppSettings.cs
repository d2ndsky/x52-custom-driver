using System;

namespace X52.CustomDriver.Core.Models
{
    public class AppSettings
    {
        public bool MinimizeToTray { get; set; } = true;
        public bool CloseToTray { get; set; } = true;
        public bool RunAtStartup { get; set; } = false;
        public bool UpgradeRequired { get; set; } = true;
    }
}

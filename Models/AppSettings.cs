using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2_auto_highlights_gui
{
    public class AppSettings
    {
        public string Cs2Path { get; set; } = @"";
        public string DemoPath { get; set; } = @"";
        public string ObsIp { get; set; } = "";
        public string ObsPort { get; set; } = "";
        public string ObsPassword { get; set; } = "";
    }
}

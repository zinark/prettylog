using System;

namespace PrettyLog.Core.DataAccess
{
    public class MachineStatusDto
    {
        public DateTime On { get; set; }
        public double CPU { get; set; }
        public double Network { get; set; }
        public double Memory { get; set; }
        public string Ip { get; set; }
    }
}
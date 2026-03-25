using System;

namespace InvisiwindCS.Models
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = "";
        public uint Pid { get; set; }
        public bool IsHidden { get; set; }
        public bool IsX86 { get; set; }
    }
}

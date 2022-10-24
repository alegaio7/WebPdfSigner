using System;

namespace DesktopModule
{
    public class WebRequestCoordinatorBaseEventArgs 
    {
        public bool Handled { get; set; }
        public bool Cancel { get; set; }
        public string CancelReason { get; set; }
    }
}
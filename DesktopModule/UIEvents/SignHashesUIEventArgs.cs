﻿using System.Collections.Generic;

namespace DesktopModule
{
    public class SignHashesUIEventArgs : WebRequestCoordinatorBaseEventArgs
    {
        public string CertificateThumbprint { get; set; }
        public List<SignHashFileInfo> FileHashesToSign { get; set; }
    }
}
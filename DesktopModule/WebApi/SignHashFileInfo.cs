using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DesktopModule
{
    public class SignHashFileInfo
    {
        public string OriginalFile { get; set; }
        public string PreparedFile { get; set; }
        public string FileHash { get; set; } // received from js client
        [JsonIgnore]
        public byte[] FileHashBin { get; set; } // don't return this to client
        public byte[] SignedFileHash { get; set; }
    }
}
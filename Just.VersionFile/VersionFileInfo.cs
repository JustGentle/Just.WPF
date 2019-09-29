using System.Collections.Generic;

namespace Just.VersionFile
{
    public class VersionFileInfo
    {
        public Dictionary<string, VersionInfo> Version { get; set; } = new Dictionary<string, VersionInfo>();

        public Dictionary<string, string> CheckData { get; set; } = new Dictionary<string, string>();
    }
    public class VersionInfo
    {
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}

namespace Just.Attachment
{
    public class StoreServerConfig
    {
        public int ID { get; set; }
        public string Server { get; set; }
        public string Root { get; set; }
        public StoreModeEnum StoreMode { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public string Suffix { get; set; }
        public bool IsEnabled { get; set; }

        public string Path
        {
            get
            {
                switch (StoreMode)
                {
                    case StoreModeEnum.Share:
                        return $@"\\{Server}\{Root.Trim('\\')}";
                    case StoreModeEnum.Local:
                    default:
                        return Root;
                }
            }
        }
    }
}

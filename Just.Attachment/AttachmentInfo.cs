using System;

namespace Just.Attachment
{
    public class AttachmentInfo
    {
        public int ID { get; set; }
        public Guid? FileReName { get; set; }
        public string Extension { get; set; }
        public string Suffix { get; set; }
        public string DirectoryPath { get; set; }
        public string Root { get; set; }
        public StoreModeEnum StoreMode { get; set; }
        public string Server { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
    }
}

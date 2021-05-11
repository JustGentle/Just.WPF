using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Just.MongoDB
{
    [BsonIgnoreExtraElements]
    public class ShortMessageSend
    {
        public ShortMessageSend()
        {
            BizId = string.Empty;
            UserID = 1;
            InStampTime = DateTime.Now;
            OutStampTime = DateTime.MinValue;
            StartSendTime = DateTime.MinValue;
            Timing = DateTime.Now;
            Status = SMSStatus.ToBeSend;
        }
        public ObjectId Id { get; set; }
        public string BizId { get; set; }
        public string Content { get; set; }
        public int UserID { get; set; }
        public string SendToMobile { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime InStampTime { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime OutStampTime { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime StartSendTime { get; set; }
        public int RetryTimes { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Timing { get; set; }
        public string RecieveName { get; set; }
        public string SMSCode { get; set; }
        public int Type { get; set; }
        public SMSStatus Status { get; set; }
    }
    /// <summary>
    /// 短信状态
    /// </summary>
    public enum SMSStatus
    {
        Success = 1,
        Fail = 2,
        ToBeSend = 3
    }
}

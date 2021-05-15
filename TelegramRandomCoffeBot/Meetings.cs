using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramRandomCoffeBot
{
    public class Meetings
    {
        public int ID { get; set; }
        public DateTime MeetingTime { get; set; }
        public int FirstUserID { get; set; }
        public int SecondUserID { get; set; }
        public MeetingState MeetingStateID { get; set; }
        public string Place { get; set; }
    }
}

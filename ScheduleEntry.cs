using System;

namespace mgt_parser
{
    [Serializable]
    public class ScheduleEntry
    {
        public readonly int Hour;
        public readonly int Minute;
        public readonly RouteType RouteType;

        public ScheduleEntry(int hour, int minute)
        {
            Hour = hour;
            Minute = minute;
            RouteType = RouteType.Normal;
        }

        public ScheduleEntry(int hour, int minute, RouteType type)
        {
            Hour = hour;
            Minute = minute;
            RouteType = type;
        }
    }
}

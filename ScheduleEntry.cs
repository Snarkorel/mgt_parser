namespace mgt_parser
{
    public class ScheduleEntry
    {
        private int _hour;
        private int _minute;
        private RouteType _type;

        public ScheduleEntry(int hour, int minute)
        {
            _hour = hour;
            _minute = minute;
            _type = RouteType.Normal;
        }

        public ScheduleEntry(int hour, int minute, RouteType type)
        {
            _hour = hour;
            _minute = minute;
            _type = type;
        }

        public int GetHour()
        {
            return _hour;
        }

        public int GetMinute()
        {
            return _minute;
        }

        public RouteType GetRouteType()
        {
            return _type;
        }
    }
}

namespace mgt_parser
{
    class Uri
    {
        public const string RouteListRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=ways&type={0}";
        public const string RouteDaysRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=days&type={0}&way={1}";
        public const string RouteDirectionsRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=directions&type={0}&way={1}&date={2}";
        public const string RoutesStopsRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=waypoints&type={0}&way={1}&date={2}&direction={3}";
        public const string ScheduleRequestUri = "http://mosgortrans.org/pass3/shedule.php?type={0}&way={1}&date={2}&direction={3}&waypoint={4}";

        public static string GetUri(string type)
        {
            return string.Format(RouteListRequestUri, type);
        }

        public static string GetUri(string type, string route)
        {
            return string.Format(RouteDaysRequestUri, type, route);
        }

        public static string GetUri(string type, string route, string days)
        {
            return string.Format(RouteDirectionsRequestUri, type, route, days);
        }

        public static string GetUri(string type, string route, string days, string direction)
        {
            return string.Format(RoutesStopsRequestUri, type, route, days, direction);
        }

        public static string GetUri(string type, string route, string days, string direction, string stop)
        {
            return string.Format(ScheduleRequestUri, type, route, days, direction, stop);
        }

        public static string GetUri(TransportType type, string route, Days days, DirectionCode direction, int stop)
        {
            return string.Format(ScheduleRequestUri, TrType.GetTypeString(type), route, days.ToString(), Direction.GetDirectionString(direction), stop);
        }
    }
}

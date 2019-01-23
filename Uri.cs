namespace mgt_parser
{
    public static class Uri
    {
        private static string _host = "213.79.91.85";
        private static string _addr;
        private static string _addrFormat = "http://{0}/pass3/";
        private static string RouteListRequestUri = "request.ajax.php?list=ways&type={0}";
        private static string RouteDaysRequestUri = "request.ajax.php?list=days&type={0}&way={1}";
        private static string RouteDirectionsRequestUri = "request.ajax.php?list=directions&type={0}&way={1}&date={2}";
        private static string RoutesStopsRequestUri = "request.ajax.php?list=waypoints&type={0}&way={1}&date={2}&direction={3}";
        private static string ScheduleRequestUri = "shedule.php?type={0}&way={1}&date={2}&direction={3}&waypoint={4}";

        static Uri()
        {
            SetHost(_host);
        }

        public static void SetHost(string host)
        {
            _host = host;
            _addr = string.Format(_addrFormat, _host);
        }

        public static string GetUri(string type)
        {
            return string.Format(_addr + RouteListRequestUri, type);
        }

        public static string GetUri(string type, string route)
        {
            return string.Format(_addr + RouteDaysRequestUri, type, route);
        }

        public static string GetUri(string type, string route, string days)
        {
            return string.Format(_addr + RouteDirectionsRequestUri, type, route, days);
        }

        public static string GetUri(string type, string route, string days, string direction)
        {
            return string.Format(_addr + RoutesStopsRequestUri, type, route, days, direction);
        }

        public static string GetUri(string type, string route, string days, string direction, string stop)
        {
            return string.Format(_addr + ScheduleRequestUri, type, route, days, direction, stop);
        }

        public static string GetUri(TransportType type, string route, Days days, DirectionCode direction, int stop)
        {
            return string.Format(_addr + ScheduleRequestUri, TrType.GetTypeString(type), route, days.ToString(), Direction.GetDirectionString(direction), stop);
        }
    }
}

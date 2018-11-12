using System;

namespace mgt_parser
{
    [Serializable]
    public class ScheduleInfo
    {
        private TransportType _transportType;
        private string _routeName;
        private Days _days;
        private DirectionCode _direction;
        private string _directionName;
        private int _stopNumber;
        private string _stopName;

        //public ScheduleInfo(TransportType type, string route, Days day, DirectionCode dir, string dirName, int stopNum, string stopName)
        //{
        //    _transportType = type;
        //    _routeName = route;
        //    _days = day;
        //    _direction = dir;
        //    _directionName = dirName;
        //    _stopNumber = stopNum;
        //    _stopName = stopName;
        //}

        //public ScheduleInfo(string type, string route, string day, string dir, string dirName, string stopNum, string stopName)
        //{
        //    _transportType = TrType.GetTransportType(type);
        //    _routeName = route;
        //    _days = new Days(day);
        //    _direction = Direction.GetDirectionCode(dir);
        //    _directionName = dirName;
        //    if (stopNum == "all")
        //        throw new Exception("Usage of \"all\" in schedule info is not allowed!");
        //    _stopNumber = Convert.ToInt32(stopNum);
        //    _stopName = stopName;
        //}

        public ScheduleInfo(string type, string route, string day, string dir, string dirName, int stopNum, string stopName)
        {
            _transportType = TrType.GetTransportType(type);
            _routeName = route;
            _days = new Days(day);
            _direction = Direction.GetDirectionCode(dir);
            _directionName = dirName;
            _stopNumber = stopNum;
            _stopName = stopName;
        }

        //ONLY FOR TEST PURPOSES!
        //public ScheduleInfo(string type, string route, string day, string dir, int stopNum)
        //{
        //    _transportType = TrType.GetTransportType(type);
        //    _routeName = route;
        //    _days = new Days(day);
        //    _direction = Direction.GetDirectionCode(dir);
        //    _stopNumber = stopNum;
        //}

        public TransportType GetTransportType()
        {
            return _transportType;
        }

        public string GetTransportTypeString()
        {
            return TrType.GetTypeString(_transportType);
        }

        public string GetRouteName()
        {
            return _routeName;
        }

        public Days GetDaysOfOperation()
        {
            return _days;
        }

        public DirectionCode GetDirectionCode()
        {
            return _direction;
        }

        public string GetDirectionCodeString()
        {
            return Direction.GetDirectionString(_direction);
        }

        public string GetDirectionName()
        {
            return _directionName;
        }

        public int GetStopNumber()
        {
            return _stopNumber;
        }

        public string GetStopName()
        {
            return _stopName;
        }
    }
}

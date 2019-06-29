using System;

namespace mgt_parser
{
    [Serializable]
    public class ScheduleInfo
    {
        public readonly TransportType TransportType;
        public readonly string RouteName;
        public readonly Days DaysOfOperation;
        public readonly DirectionCode DirCode;
        public readonly string DirectionName;
        public readonly int StopNumber;
        public readonly string StopName;

        public ScheduleInfo(string type, string route, string day, string dir, string dirName, int stopNum, string stopName)
        {
            TransportType = TrType.GetTransportType(type);
            RouteName = route;
            DaysOfOperation = new Days(day);
            DirCode = Direction.GetDirectionCode(dir);
            DirectionName = dirName;
            StopNumber = stopNum;
            StopName = stopName;
        }

        public string GetTransportTypeString()
        {
            return TransportType.GetTypeString();
        }

        public string GetDirectionCodeString()
        {
            return DirCode.GetDirectionString();
        }
    }
}

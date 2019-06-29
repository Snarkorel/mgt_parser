using System.Collections.Generic;
using System;

namespace mgt_parser
{
    [Serializable]
    public class Schedule
    {
        public ScheduleInfo Info { get; private set; }
        public List<ScheduleEntry> Entries { get; private set; }
        public DateTime ValidityTime { get; set; }
        private Dictionary<RouteType,string> _destinations; 

        public Schedule(ScheduleInfo info)
        {
            Info = info;
            Entries = new List<ScheduleEntry>();
        }

        public void AddEntry(ScheduleEntry entry)
        {
            Entries.Add(entry);
        }

        public ScheduleInfo GetInfo()
        {
            return Info;
        }

        public string GetUri()
        {
            return Uri.GetUri(Info.TransportType, Info.RouteName, Info.DaysOfOperation, Info.DirCode, Info.StopNumber);
        }

        public void SetSpecialRoute(RouteType type, string destination)
        {
            if (_destinations == null)
                _destinations = new Dictionary<RouteType, string>();
            _destinations.Add(type, destination);
        }

        public string GetSpecialRoute(RouteType type)
        {
            string val;
            try
            {
                _destinations.TryGetValue(type, out val);
            }
            catch (Exception)
            {
                val = string.Empty;
            }
            return val;
        }
    }
}

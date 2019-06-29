using System.Collections.Generic;
using System;

namespace mgt_parser
{
    [Serializable]
    public class Schedule
    {
        private ScheduleInfo _info;
        private List<ScheduleEntry> _entries;
        private DateTime _validFrom;
        private Dictionary<RouteType,string> _destinations; 

        public Schedule(ScheduleInfo info)
        {
            _info = info;
            _entries = new List<ScheduleEntry>();
        }

        public void AddEntry(ScheduleEntry entry)
        {
            _entries.Add(entry);
        }

        public List<ScheduleEntry> GetEntries()
        {
            return _entries;
        }

        public ScheduleInfo GetInfo()
        {
            return _info;
        }

        public string GetUri()
        {
            return Uri.GetUri(_info.GetTransportType(), _info.GetRouteName(), _info.GetDaysOfOperation(), _info.GetDirectionCode(), _info.GetStopNumber());
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

        public void SetValidityTime(DateTime validFrom)
        {
            _validFrom = validFrom;
        }

        public DateTime GetValidityTime()
        {
            return _validFrom;
        }
    }
}

using System.Collections.Generic;

namespace mgt_parser
{
    public class Schedule //TODO: IEnumerable
    {
        private ScheduleInfo _info;
        private List<ScheduleEntry> _entries;
        //TODO: хранить направления спецрейсов в Hashtable

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
            //TODO!
        }
    }
}

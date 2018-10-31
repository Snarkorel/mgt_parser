using System;
using System.Collections;
using System.Text;

namespace mgt_parser
{
    /*
    public enum DaysOfOperation
    {
        /// <summary>
        /// Ежедневно (1111111)
        /// </summary>
        Daily,
        /// <summary>
        /// Будни (1111100)
        /// </summary>
        Weekdays,
        /// <summary>
        /// Выходные (0000011)
        /// </summary>
        Weekend,
        /// <summary>
        /// Суббота (0000010)
        /// </summary>
        Saturday,
        /// <summary>
        /// Воскресенье (0000001)
        /// </summary>
        Sunday
    }
    */

    [Serializable]
    public class Days
    {
        private const int DayBitsLength = 7;
        BitArray _daysOfOperation;

        public Days(BitArray days)
        {
            if (days.Length != DayBitsLength)
                throw new ArgumentOutOfRangeException("Invalid days of operation bits count", days.ToString());
            _daysOfOperation = days;
        }

        public Days(string days)
        {
            if (days.Length != DayBitsLength)
                throw new ArgumentOutOfRangeException("Invalid days of operation bitmask", days);
            _daysOfOperation = new BitArray(DayBitsLength);
            for (var i = 0; i < days.Length; i++)
            {
                _daysOfOperation[i] = days[i] == '1';
            }
        }

        public BitArray GetDays()
        {
            return _daysOfOperation; //TODO: check for null?
        }

        public override string ToString()
        {
            StringBuilder bitString = new StringBuilder();
            for (var i = 0; i < _daysOfOperation.Length; i++)
            {
                bitString.Append(_daysOfOperation[i] ? '1' : '0');
            }
            return bitString.ToString();
        }
    }

    /*
    public static class Day
    {
        private const string Daily =    "1111111";
        private const string Weekdays = "1111100";
        private const string Weekend =  "0000011";
        private const string Saturday = "0000010";
        private const string Sunday =   "0000001";

        public static string GetDayString(DaysOfOperation days)
        {
            switch (days)
            {
                case DaysOfOperation.Daily: return Daily;
                case DaysOfOperation.Weekdays: return Weekdays;
                case DaysOfOperation.Weekend: return Weekend;
                case DaysOfOperation.Saturday: return Saturday;
                case DaysOfOperation.Sunday: return Sunday;
                default: throw new ArgumentOutOfRangeException(days.ToString(), "Unknown day of operation");
            }
        }

        public static DaysOfOperation GetDayCode(string days)
        {
            switch (days)
            {
                case Daily: return DaysOfOperation.Daily;
                case Weekdays: return DaysOfOperation.Weekdays;
                case Weekend: return DaysOfOperation.Weekend;
                case Saturday: return DaysOfOperation.Saturday;
                case Sunday: return DaysOfOperation.Sunday;
                default: throw new ArgumentOutOfRangeException(days, "Unknown day of operation");
            }
        }
    }*/
}

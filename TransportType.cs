using System;

namespace mgt_parser
{
    public enum TransportType
    {
        /// <summary>
        /// avto
        /// </summary>
        Bus,
        /// <summary>
        /// trol
        /// </summary>
        Trolleybus,
        /// <summary>
        /// tram
        /// </summary>
        Tram
    }

    public static class TrType
    {
        private const string Bus = "avto";
        private const string Trolleybus = "trol";
        private const string Tram = "tram";

        public static string[] TransportTypes = { Bus, Trolleybus, Tram };

        public static string GetTypeString(TransportType type)
        {
            switch (type)
            {
                case TransportType.Bus: return Bus;
                case TransportType.Trolleybus: return Trolleybus;
                case TransportType.Tram: return Tram;
                default: throw new ArgumentOutOfRangeException(type.ToString(), "Unknown transport type");
            }
        }

        public static TransportType GetTransportType(string type)
        {
            switch (type)
            {
                case Bus: return TransportType.Bus;
                case Trolleybus: return TransportType.Trolleybus;
                case Tram: return TransportType.Tram;
                default: throw new ArgumentOutOfRangeException(type, "Unknown transport type");
            }
        }
    }
}

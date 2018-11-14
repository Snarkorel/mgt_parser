using System;

namespace mgt_parser
{
    public enum RouteType
    {
        /// <summary>
        /// Just ordinary route
        /// </summary>
        Normal,
        /// <summary>
        /// Special route #1 ("Красные")
        /// </summary>
        SpecialRed,
        /// <summary>
        /// Special route #2 ("Зелёные")
        /// </summary>
        SpecialGreen,
        /// <summary>
        /// Special route #3 ("Синие")
        /// </summary>
        SpecialBlue,
        /// <summary>
        /// Special route #4 ("Розовые")
        /// </summary>
        SpecialPurple,
        /// <summary>
        /// Special route #5 ("Бежевые")
        /// </summary>
        SpecialBeige
    }

    public static class RouteTypeProvider
    {
        private const string Red = "red";
        private const string Green = "green";
        private const string Blue = "darkblue";
        private const string Purple = "#ff69b4";
        private const string Beige = "#B8860B";

        public static RouteType GetRouteType(string routeColor)
        {
            switch (routeColor)
            {
                case Red:
                    return RouteType.SpecialRed;
                case Green:
                    return RouteType.SpecialGreen;
                case Blue:
                    return RouteType.SpecialBlue;
                case Purple:
                    return RouteType.SpecialPurple;
                case Beige:
                    return RouteType.SpecialBeige;
                default:
                    throw new ArgumentOutOfRangeException(routeColor, "Unknown special route color!");
            }
        }
    }
}

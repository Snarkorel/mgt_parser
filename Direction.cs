using System;

namespace mgt_parser
{
    public enum DirectionCode
    {
        /// <summary>
        /// AB
        /// </summary>
        Forward,
        /// <summary>
        /// BA
        /// </summary>
        Backward
    }

    public class Direction
    {
        private const string Forward = "AB";
        private const string Backward = "BA";

        public static string[] Directions = { Forward, Backward };

        public static string GetDirectionString(DirectionCode dir)
        {
            switch (dir)
            {
                case DirectionCode.Forward: return Forward;
                case DirectionCode.Backward: return Backward;
                default: throw new ArgumentOutOfRangeException(dir.ToString(), "Unknown direction");
            }
        }

        public static DirectionCode GetDirectionCode(string direction)
        {
            switch (direction)
            {
                case Forward: return DirectionCode.Forward;
                case Backward: return DirectionCode.Backward;
                default: throw new ArgumentOutOfRangeException(direction, "Unknown direction");
            }
        }
    }
}

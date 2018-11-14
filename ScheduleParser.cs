using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace mgt_parser
{
    public static class ScheduleParser
    {
        //I know that parsing HTML by regex is a bad idea, but objective is not to use third-party parsers
        public static Schedule Parse(string htmlData, ScheduleInfo si)
        {
            var schedule = new Schedule(si);

            var index = 0; //Current position of htmlData processing (shifts if some item is found and parsed)
            var searchIndex = 0; //Curent position of search ahead of index

            const string ValidityTimeSearchStr = "c</h3></td><td><h3>";
            const string LegendHeaderStr = "<h3>Легенда</h3>";
            const string TagBeginning = "<";
            const string TdClosingTag = "</td>";
            const string SpanClosingTag = "</span>";
            const string NoDataForLegend = "Нет особых данных для легенды";
            const string From = "от";

            //<span class=\"hour\">(\d+)</span></td><td align=.*>(.*)</td>
            const string HourSearchStr = "<span class=\"hour\">";
            const string HourRegexPattern = "<span class=\"hour\">(\\d+)</span>"; //" < span class=\"hour\">(\\d+)</span></td><td align=.*>(.*)</td>";
            var hourRegex = new Regex(HourRegexPattern);
            //<span class="minutes" >02</span>
            //<span class="minutes" style="color: red; font-weight: bold;">37</span><br>
            const string MinutesSearchStr = "<span class=\"minutes\"";
            const string MinutesRegexPattern = "<span class=\"minutes\"( |.*color: ([a-zA-z0-9#]+).*)>(\\d+)</span>";
            var minuteRegex = new Regex(MinutesRegexPattern);

            //Starting routine
            
            searchIndex = htmlData.IndexOf(ValidityTimeSearchStr, index);
            if (searchIndex == -1)
                throw new Exception("Validity time not found!");

            index = searchIndex + ValidityTimeSearchStr.Length;
            searchIndex = htmlData.IndexOf(TagBeginning, index);
            if (searchIndex == -1)
                throw new Exception("Validity time border not found!");

            var validityStr = htmlData.Substring(index, searchIndex - index);
            var date = ParseDateTime(validityStr);
            schedule.SetValidityTime(date);
            //Console.WriteLine("Schedule valid from: " + date.ToString());
            index = searchIndex;

            //Iterative hours and minutes parsing
            do
            {
                //Parsing hours
                searchIndex = htmlData.IndexOf(HourSearchStr, index);
                if (searchIndex == -1)
                    break;
                index = searchIndex;
                searchIndex = htmlData.IndexOf(TdClosingTag, index);

                //Grey hours will be ignored because they doesn't match to regexp
                sbyte hour = -1;
                var hourStr = htmlData.Substring(index, searchIndex - index);
                var hourMatch = hourRegex.Match(hourStr);
                if (hourMatch.Length == 0)
                {
                    throw new Exception("Failed to find hour info!");
                }
                else
                {
                    hour = Convert.ToSByte(hourMatch.Groups[1].Value);
                }

                //Getting substring for all minutes in hour, then searching for all of it

                //Parsing minutes
                searchIndex = htmlData.IndexOf(MinutesSearchStr, index);
                index = searchIndex;
                var minutesBorderIndex = htmlData.IndexOf(TdClosingTag, index);
                var allMinutesStr = htmlData.Substring(index, minutesBorderIndex - index);
                var minuteIndex = 0; //local varibles for searching inside minutes substring
                var minuteSearchIndex = 0;

                while (minuteSearchIndex != -1)
                {
                    minuteSearchIndex = allMinutesStr.IndexOf(SpanClosingTag, minuteIndex);
                    if (minuteSearchIndex == -1)
                        break;
                    var minuteStr = allMinutesStr.Substring(minuteIndex, minuteSearchIndex - minuteIndex + SpanClosingTag.Length);
                    minuteIndex = minuteSearchIndex + SpanClosingTag.Length;
                    var minuteMatch = minuteRegex.Match(minuteStr);
                    if (minuteMatch.Length == 0)
                    {
                        throw new Exception("Failed to find minute info!");
                    }
                    else
                    {
                        if (hour == -1)
                            throw new Exception("Hours parser fucked up!");
                        var minute = Convert.ToSByte(minuteMatch.Groups[3].Value);
                        var specialRoute = minuteMatch.Groups[2].Value;
                        if (specialRoute.Length > 1)
                        {
                            var type = RouteTypeProvider.GetRouteType(specialRoute);
                            schedule.AddEntry(new ScheduleEntry(hour, minute, type)); //special route
                        }
                        else
                            schedule.AddEntry(new ScheduleEntry(hour, minute)); //regular route
                    }
                }

                //all minutes for current hour found, skipping to next hour
                index = minutesBorderIndex;
                searchIndex = index;
            }
            while (searchIndex > 0);

            //Parsing the legend
            searchIndex = htmlData.IndexOf(LegendHeaderStr, index);
            if (searchIndex != 0)
            {
                index = searchIndex + LegendHeaderStr.Length;
                searchIndex = htmlData.IndexOf(TdClosingTag, index);
                if (searchIndex != 0)
                {
                    var legendData = htmlData.Substring(index, searchIndex - index);

                    //.*? for non-greedy match instead of greedy .*
                    //const string noColorsRegexPattern = "<p class=\"helpfile\"><b>(.*)<\\/b>(.*)<\\/p>";
                    const string colorsRegexPattern = "<p class=\"helpfile\"><b style=\"color: ([#a-zA-Z0-9]+)\">(.*?)<\\/b>(.*?)<\\/p>";

                    //regex pattern without colors: <p class="helpfile"><b>(.*)<\/b>(.*)<\/p>
                    //group1: bold text, group2: non-bold text (check for empty!)

                    //var noColorsRegex = new Regex(noColorsRegexPattern);
                    //var matches = noColorsRegex.Matches(legendData);

                    //Console.WriteLine("Matching non-colored legend...");

                    //if (matches.Count == 0)
                    //    Console.WriteLine("Non-colored regex not matched!");
                    //else
                    //{
                    //    if (legendData.IndexOf(NoDataForLegend) == -1)
                    //    {
                    //        //normally we shouldn't be there. This output just for debugging purposes and should be removed later (TODO)
                    //        foreach (Match match in matches)
                    //        {
                    //            //Console.WriteLine("Match: " + match.Value);
                    //            //GroupCollection groups = match.Groups;
                    //            //foreach (Group group in groups)
                    //            //{
                    //            //    Console.WriteLine("Group: " + group.Value);
                    //            //}
                    //        }
                    //    }

                    //}

                    //Console.WriteLine("Matching colored legend...");
                    //regex pattern with colors: <p class="helpfile"><b style="color: (\w+)">(.*)<\/b>(.*)<\/p>
                    //should be multiple matches
                    //group1: color name, group2: color name in russian (bold text), group3: non-bold text (description)

                    var colorsRegex = new Regex(colorsRegexPattern);
                    var matches = colorsRegex.Matches(legendData);

                    if (matches.Count != 0)
                    {
                        foreach (Match match in matches)
                        {
                            //Console.WriteLine("Match: " + match.Value);
                            GroupCollection groups = match.Groups;
                            var type = RouteTypeProvider.GetRouteType(groups[1].Value);
                            var destinationRaw = groups[3].Value; //TODO: change regexp, no need in russian name of color
                            var startIndex = destinationRaw.IndexOf(From);
                            var destination = destinationRaw.Substring(startIndex);
                            schedule.SetSpecialRoute(type, destination);
                        }
                    }
                }
            }
            return schedule;
        }

        private static DateTime ParseDateTime(string dateStr)
        {
            Dictionary<string, int> months = new Dictionary<string, int> {
                {"января", 1},
                {"февраля", 2},
                {"марта", 3},
                {"апреля", 4},
                {"мая", 5},
                {"июня", 6},
                {"июля", 7},
                {"августа", 8},
                {"сентября", 9},
                {"октября", 10},
                {"ноября", 11},
                {"декабря", 12} };

            const string regexPattern = @"(\d+) (\w+) (\d+)";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            var match = regex.Match(dateStr);

            if (match.Groups.Count == 0)
                throw new Exception("Regex for date not matched!");

            //Console.WriteLine(match.Value);
            GroupCollection groups = match.Groups;

            var day = Convert.ToInt32(groups[1].Value);
            var month = months[groups[2].Value];
            var year = Convert.ToInt32(groups[3].Value);

            return new DateTime(year, month, day);
        }
    }
}

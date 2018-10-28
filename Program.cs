using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;

namespace mgt_parser
{
    class Program
    {
        private static HttpClient _client;
        private static List<Schedule> _schedules;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            _client = new HttpClient();
            _schedules = new List<Schedule>();
            //GetLists(_client);
            //Console.WriteLine("Finishing"); //TODO: wait for async completion

            //TEST
            TestScheduleParser(_client);

            while (true) { };
        }

        private static async void TestScheduleParser(HttpClient client)
        {
            //shedule.php?type=avto&way=0&date=1111100&direction=AB&waypoint=1
            var scheduleInfo = new ScheduleInfo("avto", "0", "1111100", "AB", 1); //simple case
            var schedule = await GetSchedule(_client, scheduleInfo);
          
            //shedule.php?type=avto&way=205&date=1111100&direction=AB&waypoint=2
            scheduleInfo = new ScheduleInfo("avto", "205", "1111100", "AB", 2); //complex case with different colors
            schedule = await GetSchedule(_client, scheduleInfo);

            //shedule.php?type=avto&way=%C1%CA&date=1111100&direction=AB&waypoint=1
            scheduleInfo = new ScheduleInfo("avto", "%C1%CA", "1111100", "AB", 1); //TODO: convert cyrillic chars to HTML char codes
            schedule = await GetSchedule(_client, scheduleInfo);
        }

        private static async void GetLists(HttpClient client)
        {
            var routesCount = new int[TrType.TransportTypes.Length];
            var maxStops = 0;
            string maxStopsRoute = "";
            TransportType maxStopsTransport = TransportType.Bus;

            for (var i = 0; i < TrType.TransportTypes.Length; i++)
            {
                var type = TrType.TransportTypes[i];
                Console.WriteLine("Obtaining routes for " + type);
                var routes = await GetRoutesList(_client, type);
                foreach(var route in routes)
                {
                    routesCount[i]++;
                    Console.WriteLine("\tFound route: " + route);
                    var days = await GetDaysOfOperation(client, type, route);
                    foreach(var day in days)
                    {
                        Console.WriteLine("\t\tWorks on " + day);
                        //Direction names is not necessary, using AB/BA instead for iterating
                        var directions = await GetDirections(client, type, route, day);
                        foreach(var direction in directions)
                        {
                            Console.WriteLine("\t\t\tFound direction: " + direction);
                        }

                        for(var j = 0; j < Direction.Directions.Length; j++)
                        {
                            var dir = Direction.Directions[j];
                            var direction = directions[j];
                            var stops = await GetStops(client, type, route, day, dir);

                            //some statistics
                            if (stops.Count > maxStops)
                            {
                                maxStops = stops.Count;
                                maxStopsRoute = route;
                                maxStopsTransport = (TransportType)i;
                            }

                            for (var stopNum = 0; stopNum < stops.Count; stopNum++)
                            {
                                Console.WriteLine("\t\t\t\tFound stop: " + stops[stopNum]);

                                //TODO: multithreading

                                //TODO: dont forget to set stop name and direction name (if it's not obtained earlier)

                                //TODO: parse schedule
                                //GetSchedule(client, type, route, day, dir, "all");
                                //for (var stopNum = 0; stopNum < stops.Count; stopNum++)
                                //{
                                //    GetSchedule(client, type, route, day, dir, stopNum.ToString());
                                //}

                                //test
                                //_schedules.Add(new Schedule(new ScheduleInfo(type, route, day, dir, direction, stopNum, stops[stopNum])));
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Now _schedules list should contain all found schedules");
            Console.WriteLine("Statistics:");
            for (var t = 0; t < routesCount.Length; t++)
            {
                Console.WriteLine(string.Format("Count of {0} routes: {1}", ((TransportType)t).ToString(), routesCount[t]));
            }
            Console.WriteLine(string.Format("{0} route number {1} have absolute maximum of stops count: {2}", maxStopsTransport.ToString(), maxStopsRoute, maxStops));
        }

        //I know that parsing HTML by regex is a bad idea, but objective is not to use third-party parsers
        private static Schedule ParseSchedule(string htmlData, ScheduleInfo si)
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
            const string MinutesRegexPattern = "<span class=\"minutes\".*>(\\d+)</span>";
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
            Console.WriteLine("Schedule valid from: " + date.ToString());
            index = searchIndex;

            //Stop name parsing is not necessary - we should know it from server response?

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
                    //TODO: parse minutes colors (RouteType)
                    var minuteMatch = minuteRegex.Match(minuteStr);
                    if (minuteMatch.Length == 0)
                    {
                        throw new Exception("Failed to find minute info!");
                    }
                    else
                    {
                        var minute = Convert.ToSByte(minuteMatch.Groups[1].Value);
                        if (hour == -1)
                            throw new Exception("Hours parser fucked up!");
                        schedule.AddEntry(new ScheduleEntry(hour, minute)); //TODO RouteType (color)
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
                    const string noColorsRegexPattern = "<p class=\"helpfile\"><b>(.*)<\\/b>(.*)<\\/p>";
                    const string colorsRegexPattern = "<p class=\"helpfile\"><b style=\"color: ([#a-zA-Z0-9]+)\">(.*?)<\\/b>(.*?)<\\/p>";

                    //regex should be ungreedy (*? insted of .*)
                    //regex pattern without colors: <p class="helpfile"><b>(.*)<\/b>(.*)<\/p>
                    //group1: bold text, group2: non-bold text (check for empty!)

                    var noColorsRegex = new Regex(noColorsRegexPattern);
                    var matches = noColorsRegex.Matches(legendData);

                    Console.WriteLine("Matching non-colored legend...");

                    if (matches.Count == 0)
                        Console.WriteLine("Non-colored regex not matched!");
                    else
                    {
                        if (legendData.IndexOf(NoDataForLegend) == -1)
                        {
                            //normally we shouldn't be there. This output just for debugging purposes and should be removed later (TODO)
                            foreach (Match match in matches)
                            {
                                Console.WriteLine("Match: " + match.Value);
                                GroupCollection groups = match.Groups;
                                foreach (Group group in groups)
                                {
                                    Console.WriteLine("Group: " + group.Value);
                                }
                            }
                        }
                        
                    }

                    Console.WriteLine("Matching colored legend...");
                    //regex pattern with colors: <p class="helpfile"><b style="color: (\w+)">(.*)<\/b>(.*)<\/p>
                    //should be multiple matches
                    //group1: color name, group2: color name in russian (bold text), group3: non-bold text (description)

                    var colorsRegex = new Regex(colorsRegexPattern);
                    matches = colorsRegex.Matches(legendData);

                    if (matches.Count == 0)
                        Console.WriteLine("Colored regex not matched!");
                    else
                    {
                        foreach (Match match in matches)
                        {
                            Console.WriteLine("Match: " + match.Value);
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

            Console.WriteLine(match.Value);
            GroupCollection groups = match.Groups;

            var day = Convert.ToInt32(groups[1].Value);
            var month = months[groups[2].Value];
            var year = Convert.ToInt32(groups[3].Value);

            return new DateTime(year, month, day);
        }

        private static async Task<List<string>> GetListHttpResponse(HttpClient client, string uri)
        {
            var resp = await GetHttpResponse(client, uri);
            var strArr = new List<string>(resp.Split('\n'));
            //foreach (var str in strArr)
            //{
            //    str.TrimEnd('\r', '\n');
            //}
            if (strArr[strArr.Count - 1] == string.Empty)
            {
                strArr.RemoveAt(strArr.Count - 1);
            }
            return strArr;
        }

        private static async Task<string> GetHttpResponse(HttpClient client, string uri)
        {
            Console.WriteLine("Request: " + uri);
            try
            {
                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseBody);
                if (responseBody.Length == 0)
                    Console.WriteLine("FAIL: EMPTY RESPONSE");
                return responseBody;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get response, exception: " + e.Message);
                return string.Empty;
            }
        }

        private static async Task<List<string>> GetRoutesList(HttpClient client, string type)
        {
            Console.WriteLine("Obtaining routes list");
            var uri = Uri.GetUri(type);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDaysOfOperation(HttpClient client, string type, string route)
        {
            Console.WriteLine("Obtaining days of operation list");
            var uri = Uri.GetUri(type, route);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDirections(HttpClient client, string type, string route, string days)
        {
            Console.WriteLine("Obtaining list of directions");
            var uri = Uri.GetUri(type, route, days);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetStops(HttpClient client, string type, string route, string days, string direction)
        {
            Console.WriteLine("Obtaining list of stops");
            var uri = Uri.GetUri(type, route, days, direction);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<Schedule> GetSchedule(HttpClient client, ScheduleInfo si)
        {
            Console.WriteLine("Obtaining schedule for stop");
            var uri = Uri.GetUri(si.GetTransportTypeString(), si.GetRouteName(), si.GetDaysOfOperation().ToString(), si.GetDirectionCodeString(), si.GetStopNumber().ToString());
            var response = await GetHttpResponse(client, uri);
            Console.WriteLine("Response: " + response);
            return ParseSchedule(response, si);
        }
    }
}

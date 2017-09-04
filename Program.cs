using System;
using System.Collections.Generic;
using System.Linq;
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
            var rawData = await GetSchedule(_client, "avto", "0", "1111100", "AB", "1");
            ParseSchedule(rawData);
        }

        private static async void GetLists(HttpClient client)
        {
            for (var i = 0; i < TrType.TransportTypes.Length; i++)
            {
                var type = TrType.TransportTypes[i];
                Console.WriteLine("Obtaining routes for " + type);
                var routes = await GetRoutesList(_client, type);
                foreach(var route in routes)
                {
                    Console.WriteLine("\tFound route: " + route);
                    var days = await GetDaysOfOperation(client, type, route);
                    foreach(var day in days)
                    {
                        Console.WriteLine("\t\tWorks on " + day);
                        //Direction names is not necessary, use AB/BA instead for iterating
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
                            for (var stopNum = 0; stopNum < stops.Count; stopNum++)
                            {
                                Console.WriteLine("\t\t\t\tFound stop: " + stops[stopNum]);

                                //TODO: multithreading

                                //TODO: parse schedule
                                //GetSchedule(client, type, route, day, dir, "all");
                                //for (var stopNum = 0; stopNum < stops.Count; stopNum++)
                                //{
                                //    GetSchedule(client, type, route, day, dir, stopNum.ToString());
                                //}

                                //test
                                _schedules.Add(new Schedule(new ScheduleInfo(type, route, day, dir, direction, stopNum, stops[stopNum])));
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Now _schedules list should contain all found schedules");
        }

        private static void ParseSchedule(string htmlData)
        {
            var index = 0;
            var searchIndex = 0;
            string validityStr;
            const string ValidityTimeSearchStr = "c</h3></td><td><h3>";
            
            //TODO
            Console.WriteLine("TODO TODO TODO - SCHEDULE PARSER IS NOT READY YET");


            searchIndex = htmlData.IndexOf(ValidityTimeSearchStr, index);
            if (searchIndex !=0)
            {
                index = searchIndex + ValidityTimeSearchStr.Length;
                searchIndex = htmlData.IndexOf("<", index);
                if (searchIndex != 0)
                {
                    validityStr = htmlData.Substring(index, searchIndex - index);
                    var date = ParseDateTime(validityStr);
                    index = searchIndex;
                }
            }
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

        private static async Task<string> GetSchedule(HttpClient client, string type, string route, string days, string direction, string stop)
        {
            Console.WriteLine("Obtaining schedule for stop");
            var uri = Uri.GetUri(type, route, days, direction, stop);
            var response = await GetHttpResponse(client, uri);
            Console.WriteLine("Response: " + response);
            //ParseSchedule(response);
            return response;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace mgt_parser
{
    class Program
    {
        private static string routeListRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=ways&type={0}";
        private static string routeDaysRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=days&type={0}&way={1}";
        private static string routeDirectionsRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=directions&type={0}&way={1}&date={2}";
        private static string routesStopsRequestUri = "http://mosgortrans.org/pass3/request.ajax.php?list=waypoints&type={0}&way={1}&date={2}&direction={3}";
        private static string routeScheduleRequestUri = "http://mosgortrans.org/pass3/shedule.php?type={0}&way={1}&date={2}&direction={3}&waypoint={4}";

        static string GetUri(string type)
        {
            return string.Format(routeListRequestUri, type);
        }

        static string GetUri(string type, string route)
        {
            return string.Format(routeDaysRequestUri, type, route);
        }

        static string GetUri(string type, string route, string days)
        {
            return string.Format(routeDirectionsRequestUri, type, route, days);
        }

        static string GetUri(string type, string route, string days, string direction)
        {
            return string.Format(routesStopsRequestUri, type, route, days, direction);
        }

        static string GetUri(string type, string route, string days, string direction, string stop)
        {
            return string.Format(routeScheduleRequestUri, type, route, days, direction, stop);
        }
        
        private static HttpClient _client;
        //TODO: const
        private static string[] TransportTypes = { "avto", "trol", "tram" };
        private static string[] Directions = { "AB", "BA" };

        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            _client = new HttpClient();
            GetLists(_client);
            Console.WriteLine("Finishing"); //TODO: wait for async completion
            while (true) { };
        }

        //example
        private static async void HttpRequest()
        {
            const string hostname = "http://ya.ru";
            var client = new HttpClient();
            Console.WriteLine("Asking host " + hostname);
            HttpResponseMessage response = await client.GetAsync(hostname);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);
        }

        private static async void GetLists(HttpClient client)
        {
            for (var i = 0; i < TransportTypes.Length; i++)
            {
                var type = TransportTypes[i];
                Console.WriteLine("Obtaining routes for " + type);
                var routes = await GetRoutesList(_client, type);
                foreach(var route in routes)
                {
                    Console.WriteLine("\tFound route: " + route);
                    var days = await GetDaysOfOperation(client, type, route);
                    foreach(var day in days)
                    {
                        Console.WriteLine("\t\tWorks on " + day);
                        //TODO: direction names is not necessary, use AB/BA instead
                        var directions = await GetDirections(client, type, route, day);
                        foreach(var direction in directions)
                        {
                            Console.WriteLine("\t\t\tFound direction: " + direction);
                        }

                        foreach(var dir in Directions)
                        {
                            var stops = await GetStops(client, type, route, day, dir);
                            foreach (var stop in stops)
                            {
                                Console.WriteLine("\t\t\t\tFound stops: " + stop);
                            }
                            
                            //GetSchedule(client, type, route, day, dir, "all");
                            //for (var stopNum = 0; stopNum < stops.Length; stopNum++)
                            //{
                            //    GetSchedule(client, type, route, day, dir, stopNum.ToString());
                            //}
                        }

                    }
                }
            }

        }

        private static void ParseSchedule(string htmlData)
        {
            //TODO
            Console.WriteLine("TODO TODO TODO - SCHEDULE PARSER IS NOT READY YET");
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
            var uri = GetUri(type);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDaysOfOperation(HttpClient client, string type, string route)
        {
            Console.WriteLine("Obtaining days of operation list");
            var uri = GetUri(type, route);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDirections(HttpClient client, string type, string route, string days)
        {
            Console.WriteLine("Obtaining list of directions");
            var uri = GetUri(type, route, days);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetStops(HttpClient client, string type, string route, string days, string direction)
        {
            Console.WriteLine("Obtaining list of stops");
            var uri = GetUri(type, route, days, direction);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async void GetSchedule(HttpClient client, string type, string route, string days, string direction, string stop)
        {
            Console.WriteLine("Obtaining schedule for stop");
            var uri = GetUri(type, route, days, direction, stop);
            var response = await GetHttpResponse(client, uri);
            Console.WriteLine("Response: " + response);
            ParseSchedule(response);
        }
    }
}

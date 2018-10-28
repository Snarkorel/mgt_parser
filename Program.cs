﻿using System;
using System.Collections.Generic;
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

                                var scheduleInfo = new ScheduleInfo(type, route, day, dir, direction, stopNum, stops[stopNum]);
                                var schedule = await GetSchedule(client, scheduleInfo);
                                _schedules.Add(schedule);
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
            return ScheduleParser.Parse(response, si);
        }
    }
}

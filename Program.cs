using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;

namespace mgt_parser
{
    class Program //TODO: in future this should be a class library
    {
        private static HttpClient _client;
        //private static List<Schedule> _schedules; //TODO: use DB instead of List
        private static bool _verbose; //TODO: set via command-line arguments

        static void Main(string[] args) //TODO: args - verbose (for debug output), load (for saved schedule loading and deserializing), void (for requesting server without saving output)
        {
            VerbosePrint("Starting");
            _client = new HttpClient();
            
            _verbose = true; //TEST

            var task = GetLists(_client);
            task.Wait();
            //_schedules = task.Result;

            //SINGLE SCHEDULE TEST
            //_schedules = new List<Schedule>();
            //var task = TestSingleSchedule(_client);
            //task.Wait();
            //_schedules.Add(task.Result);

            //Serialization of results
            //var time = DateTime.Now;
            //var filename = string.Format("{0}{1}{2}_{3}{4}{5}.dat", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
            //using (FileStream file = new FileStream(filename, FileMode.Create))
            //{
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    formatter.Serialize(file, _schedules);
            //    file.Close();
            //}


            //Deserialization of results - uncomment when "-load" argument will be supported
            //var filename = "20181031_201956.dat";
            //using (FileStream file = new FileStream(filename, FileMode.Open))
            //{
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    _schedules = (List<Schedule>)formatter.Deserialize(file);
            //}

            //TEST
            //var testTask = TestScheduleParser(_client);
            //testTask.Wait();

            VerbosePrint("Finishing");

            return;
        }

        private static async Task<Schedule> TestSingleSchedule(HttpClient client)
        {
            var scheduleInfo = new ScheduleInfo("avto", "205", "1111100", "AB", 2);
            var schedule = await GetSchedule(_client, scheduleInfo);
            return schedule;
        }

        private static async Task TestScheduleParser(HttpClient client)
        {
            //shedule.php?type=avto&way=0&date=1111100&direction=AB&waypoint=1
            var scheduleInfo = new ScheduleInfo("avto", "0", "1111100", "AB", 1); //simple case
            var schedule = await GetSchedule(_client, scheduleInfo);
          
            //shedule.php?type=avto&way=205&date=1111100&direction=AB&waypoint=2
            scheduleInfo = new ScheduleInfo("avto", "205", "1111100", "AB", 2); //complex case with different colors
            schedule = await GetSchedule(_client, scheduleInfo);

            //shedule.php?type=avto&way=%C1%CA&date=1111100&direction=AB&waypoint=1
            scheduleInfo = new ScheduleInfo("avto", "%C1%CA", "1111100", "AB", 1);
            schedule = await GetSchedule(_client, scheduleInfo);
        }

        private static void VerbosePrint(string str)
        {
            if (_verbose)
                Console.WriteLine(str);
        }

        private static async Task/*<List<Schedule>>*/ GetLists(HttpClient client)
        {
            //var schedules = new List<Schedule>();
            var routesCount = new int[TrType.TransportTypes.Length];
            var maxStops = 0;
            string maxStopsRoute = "";
            TransportType maxStopsTransport = TransportType.Bus;

            var time = DateTime.Now;
            var filename = string.Format("{0}{1}{2}_{3}{4}{5}.csv", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
            using (FileStream file = new FileStream(filename, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(file))
            {
                for (var i = 0; i < TrType.TransportTypes.Length; i++)
                {
                    var type = TrType.TransportTypes[i];
                    VerbosePrint("Obtaining routes for " + type);
                    var routes = await GetRoutesList(_client, type);
                    foreach (var route in routes)
                    {
                        routesCount[i]++; //TODO: remove that later

                        VerbosePrint("\tFound route: " + route);
                        var days = await GetDaysOfOperation(client, type, route);
                        if (days.Count == 0)
                            continue; //skip faulty routes without anything ("route", "streets", "stations")

                        foreach (var day in days)
                        {
                            VerbosePrint("\t\tWorks on " + day);
                            //Direction names is not necessary, using AB/BA instead for iterating
                            var directions = await GetDirections(client, type, route, day);
                            if (directions.Count == 0)
                                continue; //skip faulty routes without directions (just in case if they appear in Mosgortans schedules)

                            for (var j = 0; j < Direction.Directions.Length; j++)
                            {
                                var dirCode = Direction.Directions[j];
                                var direction = directions[j];
                                VerbosePrint("\t\t\tFound direction: " + direction);
                                var stops = await GetStops(client, type, route, day, dirCode);
                                if (stops.Count == 0)
                                    continue; //skip faulty routes without stops (this can occur when new routes are added to database, but without schedules)

                                //some statistics. TODO: remove later
                                if (stops.Count > maxStops)
                                {
                                    maxStops = stops.Count;
                                    maxStopsRoute = route;
                                    maxStopsTransport = (TransportType)i;
                                }

                                for (var stopNum = 0; stopNum < stops.Count; stopNum++)
                                {
                                    VerbosePrint("\t\t\t\tFound stop: " + stops[stopNum]);

                                    //TODO: multithreading

                                    //TEST
                                    try
                                    {
                                        var scheduleInfo = new ScheduleInfo(type, route, day, dirCode, direction, stopNum, stops[stopNum]);
                                        var schedule = await GetSchedule(client, scheduleInfo);
                                        if (schedule != null)
                                        {
                                            //schedules.Add(schedule);
                                            //var csvStr = FormatCSVString(schedule);

                                            //TEMP: save to CSV
                                            foreach(var entry in schedule.GetEntries())
                                            {
                                                var formatStr = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}";
                                                var si = schedule.GetInfo();
                                                var tType = si.GetTransportTypeString();
                                                var rName = si.GetRouteName();
                                                var ds = si.GetDaysOfOperation().ToString();
                                                var dc = si.GetDirectionCodeString();
                                                var dn = si.GetDirectionName();
                                                var snum = si.GetStopNumber();
                                                var sname = si.GetStopName();
                                                var valDat = schedule.GetValidityTime().ToString("dd.MM.yyyy");
                                                var hour = entry.GetHour();
                                                var min = entry.GetMinute();
                                                var rType = entry.GetRouteType();
                                                var rDest = schedule.GetSpecialRoute(rType);
                                                var csvStr = string.Format(formatStr, tType, rName, ds, dc, dn, snum, sname, valDat, hour, min, rType, rDest);
                                                sw.WriteLine(csvStr);
                                            }

                                            
                                        }
                                    }
                                    catch (Exception ex) //TEST
                                    {
                                        VerbosePrint("EXCEPTION OCCURED: " + ex.Message);
                                        continue;
                                    }

                                }
                            }
                        }
                    }
                }
                sw.Flush();
                sw.Close();
                file.Close();
            }

            //TODO: remove statistics later
            VerbosePrint("Now _schedules list should contain all found schedules");
            VerbosePrint("Statistics:");
            for (var t = 0; t < routesCount.Length; t++)
            {
                VerbosePrint(string.Format("Count of {0} routes: {1}", ((TransportType)t).ToString(), routesCount[t]));
            }
            VerbosePrint(string.Format("{0} route number {1} have absolute maximum of stops count: {2}", maxStopsTransport.ToString(), maxStopsRoute, maxStops));

            //return schedules;
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
            VerbosePrint("Request: " + uri);
            try
            {
                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                //VerbosePrint(responseBody);
                if (responseBody.Length == 0)
                    throw new Exception("Empty response");
                return responseBody;
            }
            catch (Exception e)
            {
                VerbosePrint("Failed to get response, exception: " + e.Message);
                return string.Empty;
            }
        }

        private static async Task<List<string>> GetRoutesList(HttpClient client, string type)
        {
            VerbosePrint("Obtaining routes list");
            var uri = Uri.GetUri(type);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDaysOfOperation(HttpClient client, string type, string route)
        {
            VerbosePrint("Obtaining days of operation list");
            var uri = Uri.GetUri(type, route);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetDirections(HttpClient client, string type, string route, string days)
        {
            VerbosePrint("Obtaining list of directions");
            var uri = Uri.GetUri(type, route, days);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static async Task<List<string>> GetStops(HttpClient client, string type, string route, string days, string direction)
        {
            VerbosePrint("Obtaining list of stops");
            var uri = Uri.GetUri(type, route, days, direction);
            var list = await GetListHttpResponse(client, uri);
            return list;
        }

        private static string EncodeCyrillicUri(string str)
        {
            var encoding = Encoding.Default;
            var bytes = encoding.GetBytes(str);

            var encoded = string.Empty;
            for (var i = 0; i < str.Length; i++)
            {
                var b = (char)bytes[i];
                if (b < 0x80) //first 128 chars is default ASCII symbols
                {
                    encoded += System.Uri.EscapeDataString(new string(b, 1));
                }
                else
                {
                    encoded += System.Uri.HexEscape(b);
                }
            }

            return encoded;
        }

        private static async Task<Schedule> GetSchedule(HttpClient client, ScheduleInfo si)
        {
            VerbosePrint("Obtaining schedule for stop");
            //encode cyrillic characters in route name, if they are present
            var name = si.GetRouteName();
            var encodedRoute = EncodeCyrillicUri(name);

            var uri = Uri.GetUri(si.GetTransportTypeString(), encodedRoute, si.GetDaysOfOperation().ToString(), si.GetDirectionCodeString(), si.GetStopNumber().ToString());
            var response = await GetHttpResponse(client, uri); //TODO: handle response errors
            if (response.Length == 0)
                return null;
            //VerbosePrint("Response: " + response);
            return ScheduleParser.Parse(response, si);
        }
    }
}

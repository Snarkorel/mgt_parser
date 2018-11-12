using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
//using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Threading;

namespace mgt_parser
{
    class Program //TODO: in future this should be a class library
    {
        private static HttpClient _client;
        //private static List<Schedule> _schedules; //TODO: use DB instead of List
        private static bool _verbose; //TODO: set via command-line arguments
        private static object _siLock = new object();
        private static object _outLock = new object();
        private static Queue<ScheduleInfo> _siQueue = new Queue<ScheduleInfo>();
        private static Queue<string> _outputQueue = new Queue<string>();
        private static Thread[] _parseThreads;
        private static Thread _outputThread;
        private static int _threadsCnt = 12; //TODO: this count should be set via command-line arguments
        private static bool _outputFinish; //TODO: use events
        private static readonly int _sleepTime = 2;

        static void Main(string[] args) //TODO: args - verbose (for debug output), threadcount
        {
            VerbosePrint("Starting");
            _client = new HttpClient();

            //_verbose = true; //TEST

            _outputThread = new Thread(OutputThread);
            _outputThread.Start();

            var _parseThreads = new Thread[_threadsCnt];
            for (var i = 0; i < _threadsCnt; i++)
            {
                _parseThreads[i] = new Thread(ParseThread);
                _parseThreads[i].Start();
            }

            var task = GetLists(_client);
            task.Wait();



            //wait for threads completion
            VerbosePrint("Waiting for parse threads completion");
            int siCnt = 0;
            do
            {
                Thread.Sleep(_sleepTime);
                lock (_siLock)
                {
                    siCnt = _siQueue.Count;
                }
            }
            while (siCnt != 0); //TODO: wait for all worker completion. Add events?
            for (var i = 0; i < _parseThreads.Length; i++)
            {
                _parseThreads[i].Abort();
            }



            VerbosePrint("Waiting for output thread completion");
            int outCnt = 0;
            do
            {
                Thread.Sleep(_sleepTime);
                lock (_outLock)
                {
                    outCnt = _outputQueue.Count;
                }
            }
            while (outCnt != 0); //TODO: wait for file write & close. Add event?
            _outputFinish = true;
            _outputThread.Abort();

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

        private static async void ParseThread() //TODO: thread aborted event
        {
            ScheduleInfo scheduleInfo = new ScheduleInfo("avto", string.Empty, "0000000", "AB", string.Empty, -1, string.Empty); //TODO: deal with default values
            while (true)
            {
                int cnt = 0;
                while (cnt == 0)
                {
                    lock (_siLock)
                    {
                        cnt = _siQueue.Count;
                        if (cnt != 0)
                            scheduleInfo = _siQueue.Dequeue();
                    }
                    Thread.Sleep(_sleepTime);
                };

                //TODO: check for null?
                var schedule = await GetSchedule(_client, scheduleInfo);
                if (schedule != null)
                {
                    foreach (var entry in schedule.GetEntries())
                    {
                        var formatStr = "{0};{1};{2};{3};'{4}';{5};'{6}';{7};{8};{9};{10};'{11}'";
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

                        lock (_outputQueue)
                        {
                            _outputQueue.Enqueue(csvStr);
                        }
                    }
                }
            }
        }

        private static void OutputThread() //TODO: thread aborted event
        {
            var time = DateTime.Now;
            var filename = string.Format("{0}{1}{2}_{3}{4}{5}.csv", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

            using (FileStream file = new FileStream(filename, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(file))
            {
                while (true)
                {
                    int cnt = 0;
                    while (cnt == 0)
                    {
                        lock (_outLock)
                        {
                            cnt = _outputQueue.Count;
                            if (cnt != 0)
                            {
                                var csvStr = _outputQueue.Dequeue();
                                sw.WriteLine(csvStr);
                            }
                        }
                        Thread.Sleep(_sleepTime);
                    };
                    
                    if (_outputFinish)
                        break;
                };

                sw.Flush();
                sw.Close();
                file.Close();
            }
        }

        //private static async Task<Schedule> TestSingleSchedule(HttpClient client)
        //{
        //    var scheduleInfo = new ScheduleInfo("avto", "205", "1111100", "AB", 2);
        //    var schedule = await GetSchedule(_client, scheduleInfo);
        //    return schedule;
        //}

        //private static async Task TestScheduleParser(HttpClient client)
        //{
        //    //shedule.php?type=avto&way=0&date=1111100&direction=AB&waypoint=1
        //    var scheduleInfo = new ScheduleInfo("avto", "0", "1111100", "AB", 1); //simple case
        //    var schedule = await GetSchedule(_client, scheduleInfo);
          
        //    //shedule.php?type=avto&way=205&date=1111100&direction=AB&waypoint=2
        //    scheduleInfo = new ScheduleInfo("avto", "205", "1111100", "AB", 2); //complex case with different colors
        //    schedule = await GetSchedule(_client, scheduleInfo);

        //    //shedule.php?type=avto&way=%C1%CA&date=1111100&direction=AB&waypoint=1
        //    scheduleInfo = new ScheduleInfo("avto", "%C1%CA", "1111100", "AB", 1);
        //    schedule = await GetSchedule(_client, scheduleInfo);
        //}

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

                                try
                                {
                                    var scheduleInfo = new ScheduleInfo(type, route, day, dirCode, direction, stopNum, stops[stopNum]);
                                    lock (_siLock)
                                    {
                                        _siQueue.Enqueue(scheduleInfo);
                                    }
                                    Thread.Sleep(_sleepTime);
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

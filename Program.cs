using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Threading;

namespace mgt_parser
{
    class Program //TODO: string to resources, localization
    {
        private static HttpClient[] _clients;
        private static bool _verbose;
        private static object _siLock = new object();
        private static object _outLock = new object();
        private static Queue<ScheduleInfo> _siQueue = new Queue<ScheduleInfo>();
        private static Queue<string> _outputQueue = new Queue<string>();
        private static Thread[] _parseThreads;
        private static Thread _outputThread;
        private static int _threadsCnt = 2;
        private static bool _parseFinish;
        private static bool _outputFinish; //TODO: use events?
        private static int _sleepTime;
        private static int _abortedCnt;
        private static object _abortedLock = new object();

        static void Main(string[] args)
        {
            PrintMan();
            ParseCommandLineArguments(args);

            VerbosePrint("Starting");
            _clients = new HttpClient[_threadsCnt + 1];

            _outputThread = new Thread(OutputThread);
            _outputThread.Start();

            var _parseThreads = new Thread[_threadsCnt];
            for (var i = 0; i < _threadsCnt; i++)
            {
                _parseThreads[i] = new Thread(ParseThread);
                _clients[i] = new HttpClient();
                _parseThreads[i].Start(_clients[i]);
            }

            _clients[_threadsCnt] = new HttpClient();
            var task = GetLists(_clients[_threadsCnt]);
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
            while (siCnt != 0);
            _parseFinish = true;

            while (_abortedCnt != _threadsCnt)
                Thread.Sleep(_sleepTime);
            
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
            while (outCnt != 0);
            _outputFinish = true;

            while (_outputThread.IsAlive)
            {
                Thread.Sleep(_sleepTime);
            }

            VerbosePrint("Finishing");

            return;
        }

        private static void PrintMan()
        {
            Console.WriteLine("Mosgortrans schedule parser with CSV output. Avaliable parameters:");
            Console.WriteLine("-verbose: for detailed output to console (default: false);");
            Console.WriteLine("-threads <count>: for threads count setup (default: 2, recommended: 8)");
            Console.WriteLine("-timeout <milliseconds>: for threads timeout setup (default: 0)");
            Console.WriteLine("Unknown parameters will be ignored.");
            Console.WriteLine();
        }

        private static void ParseCommandLineArguments(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var str = args[i].ToLower();
                str.Trim();
                if (str == "-verbose") //default: false
                    _verbose = true;
                if (str == "-timeout") //default: 0
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException(args[i]);
                    int.TryParse(args[i + 1], out _sleepTime);
                }
                if (str == "-threads") //default: 2
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException(args[i]);
                    int.TryParse(args[i + 1], out _threadsCnt);
                }
            }
        }

        private static async void ParseThread(object clientParam)
        {
            var client = clientParam as HttpClient;
            ScheduleInfo scheduleInfo = new ScheduleInfo("avto", string.Empty, "0000000", "AB", string.Empty, -1, string.Empty); //TODO: deal with default values
            var formatStr = "{0};{1};{2};{3};'{4}';{5};'{6}';{7};{8};{9};{10};'{11}'";
            int cnt = 0;
            while (true)
            {
                Thread.Sleep(_sleepTime);

                lock (_siLock)
                {
                    cnt = _siQueue.Count;
                    if (cnt != 0)
                        scheduleInfo = _siQueue.Dequeue();
                }

                if (_parseFinish && cnt == 0) //Thread finish condition
                    break;

                if (cnt == 0)
                {
                    continue;
                }

                var schedule = await GetSchedule(client, scheduleInfo);
                if (schedule == null)
                    continue;

                foreach (var entry in schedule.GetEntries())
                {
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

                    lock (_outLock)
                    {
                        _outputQueue.Enqueue(csvStr);
                    }
                }
            }
            try
            {
                Thread.CurrentThread.Abort();
            }
            catch (ThreadAbortException)
            {
                lock(_abortedLock)
                {
                    _abortedCnt++;
                }
            }
            
        }

        private static async void OutputThread()
        {
            var time = DateTime.Now;
            var filename = string.Format("{0:D4}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}.csv", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

            using (FileStream file = new FileStream(filename, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(file, Encoding.UTF8, 65535))
            {
                int cnt = 0;
                var csvStr = string.Empty;
                while (true)
                {
                    Thread.Sleep(_sleepTime);

                    lock (_outLock)
                    {
                        cnt = _outputQueue.Count;
                        if (cnt != 0)
                        {
                            csvStr = _outputQueue.Dequeue();
                        }
                            
                    }

                    if (_outputFinish && cnt == 0) //Thread finish condition
                        break;

                    if (cnt == 0)
                        continue;

                    if (!string.IsNullOrEmpty(csvStr))
                        sw.WriteLine(csvStr);
                };

                sw.Flush();
                sw.Close();
                file.Close();
            }
            Thread.CurrentThread.Abort();
        }

        private static void VerbosePrint(string str)
        {
            if (_verbose)
                Console.WriteLine(str);
        }

        private static async Task GetLists(HttpClient client)
        {           
            for (var i = 0; i < TrType.TransportTypes.Length; i++)
            {
                var type = TrType.TransportTypes[i];
                VerbosePrint("Obtaining routes for " + type);
                var routes = await GetRoutesList(client, type);
                foreach (var route in routes)
                {
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
                                catch (Exception ex) //If we got exception - log it, and skip faulty item
                                {
                                    Console.WriteLine("EXCEPTION OCCURED: " + ex.Message);
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
            var response = await GetHttpResponse(client, uri);
            if (response.Length == 0)
                return null;

            return ScheduleParser.Parse(response, si);
        }
    }
}

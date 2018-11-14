# Mosgortrans schedule parser

## Application description

Win32 console utility that parses Mosgortrans [official schedules](http://mosgortrans.org/pass3) and provides output in CSV format.

Available command-line arguments:
* `-verbose` for detailed output (default: false);
* `-threads <count>` for thread count setup (default: 2);
* `-timeout <milliseconds>` for thread sleep time setup (default: 0);

Example of usage:
`C:\dev\mgt_parser.exe -verbose -threads 8`

## CSV format description

Each trip is represented as a string in CSV format:
`Transport type; Route name; Days of operation bitmask; Direction code; 'Direction name'; Stop number; 'Stop name'; Valid from; Hour; Minute; Route type; 'Special Direction'`

Semicolon `;` used as separator, some string included in `'` quotes.

* *Transport type* can be *avto* (bus), *trol* (trolleybus) and *tram*;
* *Route name* is the name of route, e.g. *3*, *Н4*, *400э* etc.
* *Days of operation* is a bitmask, where each bit represents day in week. For example, route with day of operation code *0000011* works only on weekends;
* *Direction code* can be *AB* for forward direction and *BA* for backward;
* *Direction name* is a string that includes name of first and last stops in route;
* *Stop number* is a number of stop in route (begins from zero);
* *Stop name*;
* *Valid from* time;
* *Hour*;
* *Minute*;
* *Route type* is a enum. RouteNormal used for regular direction, also there are SpecialRed, SpecialBlue, SpecialGreen, SpecialBeige and SpecialPurple for special directions of current route;
* *Special direction* is a string, it's empty for regular route direction;

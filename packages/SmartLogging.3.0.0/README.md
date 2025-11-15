# SmartLogging
Easy to use .NET logging framework using JSON as output format.
Log entries can be written to a file, to the console and to a stream.

Since the log entries created by this framework are in JSON format you
can process these entries programmatically with any JSON deserializer.

For Windows there is an accompanying project called *SmartLogReader* 
which lets you get the most out of your log files:
https://github.com/wolfoerster/SmartLogReader

## Overview
The SmartLogging package exports two main classes: SmartLogger and LogWriter.

### SmartLogger
SmartLogger is used to create log entries with certain log levels.

A log entry contains the following information:

1. creation time of the entry
2. log level (a value between 0 and 6)
3. log context (usually the name of the calling class)
4. log method (usually the name of the calling method)
5. log message (a simple string or the JSON representation of an object)
6. additional info, e.g. the id of the calling thread

### LogWriter
LogWriter lets you specify where the log entries go to and what's the minimum
log level to be processed. You can read more about LogWriter at the end of this
document.

In most cases you will be happy with the default settings of LogWriter 
so you won't have to deal with this class except for one method:

### LogWriter.Flush()

LogWriter caches log entries before they are written to disk or any other output
channel. So if your application terminates there might be a few entries 
in the cache which you will not see in the output. To avoid this call 
`LogWriter.Flush()` whenever you think that your application stops.

### LogLevel
The enum LogLevel defines 7 levels:

***Verbose (0)***: Logs that contain the most detailed messages. These messages may 
contain sensitive application data. These messages are disabled by default and 
should never be enabled in a production environment.

***Debug (1)***: Logs that are used for interactive investigation during development. 
These logs should primarily contain information useful for debugging and have 
no long-term value.

***Information (2)***: Logs that track the general flow of the application. These 
logs should have long-term value.

***Warning (3)***: Logs that highlight an abnormal or unexpected event in the 
application flow, but do not otherwise cause the application execution to stop.

***Error (4)***: Logs that highlight when the current flow of execution is stopped due 
to a failure. These should indicate a failure in the current activity, not an 
application-wide failure.

***Fatal (5)***: Logs that describe an unrecoverable application or system crash, or a 
catastrophic failure that requires immediate attention.

***None (6)***: Logs with this priority will be written to disk in any case. This level 
should not be used in production code. It is but being used when the LogWriter 
starts working and logs the first message "Start logging".

### Usage
You will use a single SmartLogger in each class which has to create log entries.

You can declare this logger as static so every instance of this class will share
the same logger. Since there is only one LogWriter per application, this is fine. 
So in your class you should declare something like this:

`private static readonly SmartLogger Log = new();`

And when you want to create a log entry inside a method you will call:

`Log.Information("this message is for you");`

The resulting log entry looks like this:

`{"Time":"2025-07-10T08:55:55.3391580Z","ThreadId":1,"Level":"Information",
"Context":"TestApp.Program","Method":"Main","Message":"this message is for you"}`

The log context is extracted from the declaration of the logger 
and the method name is extracted from the actual logging statement.

Even if you call any of the SmartLogger's methods without parameter, e.g.

`Log.Warning()`

you will have a log entry which shows the time, the class name and the method name.

### Logging Objects
The nice thing about SmartLogger is that it not only logs simple strings but also 
objects. If you do a call like this:

`Log.Information(DateTime.Now);`

then the JSON-serialized DateTime object will appear as message in your log entry:

`{"Time":"2025-07-09T12:49:18.9541781Z","ThreadId":1,"Level":"Information",
"Context":"TestApp.Program","Method":"Main",
"Message":"\"2025-07-09T14:49:18.9412736+02:00\""}`

This is especially usefull when logging parameters of a method.
Consider the following method:

`void DoSomething(string name, double age)`

If a logger can only log simple strings you would have to do this:

`Log.Information($"name={name},age={age}");`

With SmartLogger you can simply do this:

`Log.Information(new {name, age})`

You will get this log entry:

`{"Time":"2025-07-09T13:23:22.9126806Z",
"ThreadId":1,"Level":"Information","Context":"TestApp.Program",
"Method":"DoSomething","Message":"{\"name\":\"asd\",\"age\":123}"}`

## LogWriter Details
If you don't explicitely call one of the LogWriter.Init() methods,
your log entries will be written to a file called *"MyApp.log"* in the current 
user's temporary directory, the maximum file size will be 16 MB and the 
minimum log level will be *Information*. 

The name *"MyApp"* just stands for the name of your application.

If the log file exceeds the maximum size it is copied to a 
file called *"MyApp.log.log"* and the original file is cleared.

If you want to change this default behaviour you can call one of the 
following Init methods.

### LogWriter.Init(string fileName = null, long maxFileSize = 16MB)

If you call this method without specifying fileName and maxFileSize LogWriter
will use the same defaults as described above. The parameters fileName and
maxFileSize let you specify the log file and its maximum allowed size.

Note that the lowest accepted maxFileSize is 64 kB and the highest is 64 MB.

### LogWriter.Init(LogSettings settings)

This method lets you specify where log entries go to (file and/or console and/or 
stream and/or queue), how long entries are buffered and which entries are 
processed at all.

### LogWriter.Flush()

Forces all cached log entries to be sent to the output. If you don't call this 
method when your application terminates you might loose some log entries.

### LogWriter.MinimumLogLevel

You can change the minimum log level at any time using this property. Its value 
controls whether log entries are processed, i.e. written to an output.

### LogWriter.BufferingTime

You can also change the buffering time at any time using this property. By default
log entries are cached 0.9 seconds before they are sent to the output. You can 
change this amount of time to your needs in the range of 0.1 to 10.0 seconds.

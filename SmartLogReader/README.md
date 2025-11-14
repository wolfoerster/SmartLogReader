# SmartLogReader

A powerful Windows application to view and analyze log files.

You might also want to have a look at the accompanying project called 
*SmartLoggging*: https://github.com/wolfoerster/SmartLogging

## Key Features

### Tabular View

Log entries are horrible to be viewed in a text editor. Each logging framework
has its own format to put as much information as required into a single line
of text. Searching for specific information just by looking at the raw data 
is nearly impossible.

Most logging frameworks use a common set of properties for their log entries:

1. the time of the entry
2. the log level of the entry
3. the log context (usually the name of the class who is logging)
4. the log method  (usually the name of the method who is logging)
5. the log message (the actual message to be logged)

SmartLogReader tries to identify each of these properties and shows them in a 
tabular view. You can specify which property you want to see and how much space
this property is allowed to occupy.

### Extensible Parsers

SmartLogReader has a number of built-in log file parsers for several logging
frameworks. If the files that you want to analyze cannot be interpreted by 
SmartLogReader just implement a new class derived from ByteParser and add it
to the ParserFactory.

### Live View

When opening a log file which is updated by its application while you are
looking at it, the view will be refreshed instantly and you will always see 
the latest entries of the file.

### Filter Entries

You can set include and exclude filters to the entries you would like to see.

For example you can specify that you don't want to see any log entry with a 
log context (in most cases the class name) of "ThisAnnoyingClass".

Or you can specify to show only log entries where the log message contains 
the string "this is interesting".

### Split Views

The view of a log file can be split. In this case you can see the log entries 
in two tables stacked upon each other.

You can set different filters to these views so that for example the upper view 
shows all entries while the lower one only shows entries with a certain context.

### Multiple Views and Synchronization

You can open up to three log files in SmartLogReader. 

If those files belong together in some way (e.g. a client application which calls
two services) then it makes sense to identify which log entry is followed by which 
one in the other file.

So if you select a log entry in one of the views SmartLogReader will look for the
entry in all other views which is nearest in time to the selected entry and select 
this entry, too.
## Logging into files

Example configs for logging into files (and reading files using `Area52.Ufo` or `Area52.Tool`).

### Serilog

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.File" ],
  "MinimumLevel": {
      "Default": "Debug",
      "Override": {
         "Microsoft": "Information",
         "System": "Information",
         "Microsoft.Hosting.Lifetime": "Information"
      }
  },
  "WriteTo": [
    {
      "Name": "File",
      "Args": {
        "path": "Logs/Area52App.clef.log",
        "fileSizeLimitBytes": 10485760,
        "rollOnFileSizeLimit": true,
        "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
      }
    }
  ],
  "Enrich": [ "FromLogContext" ],
  "Destructure": [
    {
      "Name": "ToMaximumDepth",
      "Args": { "maximumDestructuringDepth": 4 }
    },
    {
      "Name": "ToMaximumStringLength",
      "Args": { "maximumStringLength": 100 }
    },
    {
      "Name": "ToMaximumCollectionCount",
      "Args": { "maximumCollectionCount": 10 }
    }
  ],
  "Properties": {
    "Application": "Area52App"
  }
},
```

## NLog

```xml
<target name="Clef"
		type="File"
		encoding="utf-8"
		lineEnding="Default"
		fileName="${basedir}/Area52App.clef.log"
		archiveFileName="${basedir}/Area52App.clef.{#####}.log"
		archiveAboveSize="10240000"
		archiveNumbering="Sequence"
		maxArchiveFiles="100"
		deleteOldFileOnStartup="false"
		enableFileDelete="true"
		createDirs="true"
		concurrentWrites="true"
		autoFlush="true"
		keepFileOpen="true"
        >
	<layout xsi:type="JsonLayout" 
			includeEventProperties="true" 
			includeGdc="true" 
			includeAllProperties="true" 
			includeMdlc="true"  
			excludeProperties=""
			suppressSpaces="true">
		<attribute name="@t" layout="${date:format=o}" />
		<attribute name="@l" layout="${level}"/>
		<attribute name="@mt" layout="${message:raw=true}" />
		<attribute name="@m" layout="${message}" />
		<attribute name="@x" layout="${exception:format=tostring}" />
		<attribute name="@i" layout="${event-properties:item=EventId}" encode="false" />
		<attribute name="Application" layout="Area52App" />
		<attribute name="MachineName" layout="${machinename}" />
		<attribute name="ProcessId" layout="${processid}" />
        <attribute name="AppVersion" layout="${assembly-version}" />
        <attribute name="SourceContext" layout="${logger}" />
        <!-- other -->
	</layout>
</target>
```

For number attributes use `encode="false"`.

dotnet fantomas .
dotnet build -c Release -p:Version=%1 -p:FileVersion=%1
for %%p in (Akkling Akkling.Cluster.Sharding Akkling.DistributedData Akkling.Hocon Akkling.Persistence Akkling.Streams Akkling.TestKit.Xunit Akkling.TestKit.Xunit2 Akkling.Streams.TestKit.Xunit Akkling.Streams.TestKit.Xunit2) do (
    dotnet nuget push ./src/%%p/bin/Release/%%p.%1.nupkg --source https://api.nuget.org/v3/index.json --api-key %2
)

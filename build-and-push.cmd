dotnet build -c Release -p:Version=%1 -p:FileVersion=%1
for %%p in (Akkling Akkling.Cluster.Sharding Akkling.DistributedData Akkling.Hocon Akkling.Persistence Akkling.Streams Akkling.TestKit Akkling.Streams.TestKit) do (
    dotnet nuget push ./src/%%p/bin/Release/%%p.%1.nupkg --source https://api.nuget.org/v3/index.json --api-key %2
)

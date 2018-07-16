REM Install .NET Core (https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Channel Current"

SET PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%
dotnet restore Akkling.fsproj
dotnet restore Akkling.TestKit.fsproj
dotnet restore Akkling.Streams.fsproj
dotnet restore Akkling.Streams.TestKit.fsproj
dotnet restore Akkling.Persistence.fsproj
dotnet restore Akkling.DistributedData.fsproj
dotnet restore Akkling.Cluster.Sharding.fsproj
dotnet restore .\tests\Akkling.Tests.fsproj
dotnet fake %*
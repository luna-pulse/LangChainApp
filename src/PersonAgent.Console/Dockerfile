FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/PersonAgent.Core/PersonAgent.Core.csproj src/PersonAgent.Core/packages.lock.json src/PersonAgent.Core/
COPY src/PersonAgent.Console/PersonAgent.Console.csproj src/PersonAgent.Console/packages.lock.json src/PersonAgent.Console/
RUN dotnet restore src/PersonAgent.Console/PersonAgent.Console.csproj --locked-mode
COPY src/PersonAgent.Core/ src/PersonAgent.Core/
COPY src/PersonAgent.Console/ src/PersonAgent.Console/
COPY Prompts/system.prompt Prompts/system.prompt
RUN dotnet publish src/PersonAgent.Console/PersonAgent.Console.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0.13
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PersonAgent.Console.dll"]

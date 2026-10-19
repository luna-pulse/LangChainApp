FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/PersonApi/PersonApi.csproj src/PersonApi/packages.lock.json src/PersonApi/
RUN dotnet restore src/PersonApi/PersonApi.csproj --locked-mode
COPY src/PersonApi/ src/PersonApi/
RUN dotnet publish src/PersonApi/PersonApi.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
ENTRYPOINT ["dotnet", "PersonApi.dll"]

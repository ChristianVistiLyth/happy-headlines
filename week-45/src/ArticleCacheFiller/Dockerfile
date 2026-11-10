# Build context is the src folder, so the shared Monitoring library is included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY ArticleCacheFiller/ArticleCacheFiller.csproj ArticleCacheFiller/
RUN dotnet restore ArticleCacheFiller/ArticleCacheFiller.csproj
COPY Monitoring/ Monitoring/
COPY ArticleCacheFiller/ ArticleCacheFiller/
RUN dotnet publish ArticleCacheFiller/ArticleCacheFiller.csproj -c Release -o /app --no-restore

# Runtime stage: the ASP.NET runtime image, because the shared Monitoring library uses ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "ArticleCacheFiller.dll"]

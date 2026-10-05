# Build context is the src folder, so the shared Monitoring and Messages libraries are included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY Messages/Messages.csproj Messages/
COPY ArticleService/ArticleService.csproj ArticleService/
RUN dotnet restore ArticleService/ArticleService.csproj
COPY Monitoring/ Monitoring/
COPY Messages/ Messages/
COPY ArticleService/ ArticleService/
RUN dotnet publish ArticleService/ArticleService.csproj -c Release -o /app --no-restore

# Runtime stage: only the published app on the smaller ASP.NET runtime image (listens on port 8080)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "ArticleService.dll"]

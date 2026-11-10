# Build context is the src folder, so the shared Monitoring and Messages libraries are included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY Messages/Messages.csproj Messages/
COPY PublisherService/PublisherService.csproj PublisherService/
RUN dotnet restore PublisherService/PublisherService.csproj
COPY Monitoring/ Monitoring/
COPY Messages/ Messages/
COPY PublisherService/ PublisherService/
RUN dotnet publish PublisherService/PublisherService.csproj -c Release -o /app --no-restore

# Runtime stage: only the published app on the small Alpine-based ASP.NET runtime image (listens on port 8080).
# GAF "Reduce Bundle Size": Alpine instead of the Debian-based default roughly halves the image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "PublisherService.dll"]

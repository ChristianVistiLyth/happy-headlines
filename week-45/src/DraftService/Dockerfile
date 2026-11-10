# Build context is the src folder, so the shared Monitoring library is included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY DraftService/DraftService.csproj DraftService/
RUN dotnet restore DraftService/DraftService.csproj
COPY Monitoring/ Monitoring/
COPY DraftService/ DraftService/
RUN dotnet publish DraftService/DraftService.csproj -c Release -o /app --no-restore

# Runtime stage: only the published app on the small Alpine-based ASP.NET runtime image (listens on port 8080).
# GAF "Reduce Bundle Size": Alpine instead of the Debian-based default roughly halves the image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "DraftService.dll"]

# Build context is the src folder, so the shared Monitoring library is included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY ProfanityService/ProfanityService.csproj ProfanityService/
RUN dotnet restore ProfanityService/ProfanityService.csproj
COPY Monitoring/ Monitoring/
COPY ProfanityService/ ProfanityService/
RUN dotnet publish ProfanityService/ProfanityService.csproj -c Release -o /app --no-restore

# Runtime stage: only the published app on the small Alpine-based ASP.NET runtime image (listens on port 8080).
# GAF "Reduce Bundle Size": Alpine instead of the Debian-based default roughly halves the image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "ProfanityService.dll"]

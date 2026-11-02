# Build context is the src folder, so the shared Monitoring library is included
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Monitoring/Monitoring.csproj Monitoring/
COPY CommentService/CommentService.csproj CommentService/
RUN dotnet restore CommentService/CommentService.csproj
COPY Monitoring/ Monitoring/
COPY CommentService/ CommentService/
RUN dotnet publish CommentService/CommentService.csproj -c Release -o /app --no-restore

# Runtime stage: only the published app on the smaller ASP.NET runtime image (listens on port 8080)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "CommentService.dll"]

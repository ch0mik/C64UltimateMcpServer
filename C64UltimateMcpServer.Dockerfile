# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ./ ./

# Restore dependencies
RUN dotnet restore ./C64UltimateMcpServer/C64UltimateMcpServer.csproj

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/C64UltimateMcpServer"
RUN dotnet build "C64UltimateMcpServer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "C64UltimateMcpServer.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish --chown=app:app /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV Ultimate__BaseUrl=http://192.168.0.120
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

USER app

# Entry point
ENTRYPOINT ["dotnet", "C64UltimateMcpServer.dll"]

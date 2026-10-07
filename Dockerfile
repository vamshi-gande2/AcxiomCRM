# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY AcxiomCRM.slnx ./
COPY AcxiomCRM.Core/AcxiomCRM.Core.csproj AcxiomCRM.Core/
COPY AcxiomCRM.Web/AcxiomCRM.Web.csproj AcxiomCRM.Web/
COPY AcxiomCRM.Tests/AcxiomCRM.Tests.csproj AcxiomCRM.Tests/

RUN dotnet restore AcxiomCRM.slnx

# Copy everything else
COPY . .

# Run tests during docker build to ensure integrity
RUN dotnet test AcxiomCRM.Tests/AcxiomCRM.Tests.csproj --no-restore -c Release

# Publish Web application
WORKDIR /src/AcxiomCRM.Web
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose standard web port
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "AcxiomCRM.Web.dll"]

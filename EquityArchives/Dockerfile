# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
#WORKDIR /home/site/wwwroot
#EXPOSE 8080

# Use x86 platform explicitly
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080



# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EquityArchives/QuantisFunctions.csproj", "EquityArchives/"]
RUN dotnet restore "./EquityArchives/QuantisFunctions.csproj"
COPY . .
WORKDIR "/src/EquityArchives"
RUN dotnet build "./QuantisFunctions.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./QuantisFunctions.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
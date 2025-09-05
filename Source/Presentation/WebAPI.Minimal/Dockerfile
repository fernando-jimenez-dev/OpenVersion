# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for better caching
COPY Source/Core/Application/Application.csproj Source/Core/Application/
COPY Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj Source/Presentation/WebAPI.Minimal/

RUN dotnet restore Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj

# Copy the rest of the source
COPY . .

RUN dotnet publish Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Default to port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebAPI.Minimal.dll"]


# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BrainStormEra.csproj", "./"]
RUN dotnet restore --use-current-runtime
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BrainStormEra.dll"]

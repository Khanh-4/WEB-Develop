FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore separately so Docker can cache the layer
COPY web/*.csproj .
RUN dotnet restore
COPY web/ .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "TechSpecs.dll"]

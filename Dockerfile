FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VigiaPlatform.API/VigiaPlatform.API.csproj VigiaPlatform.API/
RUN dotnet restore VigiaPlatform.API/VigiaPlatform.API.csproj
COPY VigiaPlatform.API/ VigiaPlatform.API/
RUN dotnet publish VigiaPlatform.API/VigiaPlatform.API.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /var/lib/vigia/keys && chown -R $APP_UID:$APP_UID /var/lib/vigia && chmod 700 /var/lib/vigia/keys
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VigiaPlatform.API.dll"]

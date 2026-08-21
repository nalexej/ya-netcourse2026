FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем всё решение
COPY EventMgtService.sln .
COPY EventMgtApi.Contracts/ EventMgtApi.Contracts/
COPY EventMgtApi.BookingsService/ EventMgtApi.BookingsService/

# Восстанавливаем зависимости
RUN dotnet publish EventMgtApi.BookingsService/EventMgtApi.BookingsService.Web/EventMgtApi.BookingsService.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventMgtApi.BookingsService.Web.dll"]

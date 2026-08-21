FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем всё решение
COPY EventMgtService.sln .
COPY EventMgtApi.Contracts/ EventMgtApi.Contracts/
COPY EventMgtApi.UsersService/ EventMgtApi.UsersService/

# Восстанавливаем зависимости и публикуем
RUN dotnet publish EventMgtApi.UsersService/EventMgtApi.UsersService.Web/EventMgtApi.UsersService.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventMgtApi.UsersService.Web.dll"]

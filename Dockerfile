FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props ./
COPY Bookstore.Api/Bookstore.Api.csproj Bookstore.Api/
RUN dotnet restore Bookstore.Api/Bookstore.Api.csproj
COPY Bookstore.Api/ Bookstore.Api/
RUN dotnet publish Bookstore.Api/Bookstore.Api.csproj --no-restore --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]

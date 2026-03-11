FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MesTech.WebApi/MesTech.WebApi.csproj", "src/MesTech.WebApi/"]
COPY ["src/MesTech.Application/MesTech.Application.csproj", "src/MesTech.Application/"]
COPY ["src/MesTech.Domain/MesTech.Domain.csproj", "src/MesTech.Domain/"]
COPY ["src/MesTech.Infrastructure/MesTech.Infrastructure.csproj", "src/MesTech.Infrastructure/"]
RUN dotnet restore "src/MesTech.WebApi/MesTech.WebApi.csproj"
COPY . .
WORKDIR "/src/src/MesTech.WebApi"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5100

USER $APP_UID

ENTRYPOINT ["dotnet", "MesTech.WebApi.dll"]

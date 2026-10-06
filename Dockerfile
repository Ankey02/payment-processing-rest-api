FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/PayApi.Domain/PayApi.Domain.csproj src/PayApi.Domain/
COPY src/PayApi.Application/PayApi.Application.csproj src/PayApi.Application/
COPY src/PayApi.Infrastructure/PayApi.Infrastructure.csproj src/PayApi.Infrastructure/
COPY src/PayApi.Api/PayApi.Api.csproj src/PayApi.Api/
RUN dotnet restore src/PayApi.Api/PayApi.Api.csproj
COPY src/ src/
RUN dotnet publish src/PayApi.Api/PayApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PayApi.Api.dll"]

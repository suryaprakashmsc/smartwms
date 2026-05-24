FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Domain/Inventory.Domain.csproj", "Domain/"]
COPY ["Application/Inventory.Application.csproj", "Application/"]
COPY ["Infrastructure/Inventory.Infrastructure.csproj", "Infrastructure/"]
COPY ["API/Inventory.API.csproj", "API/"]

RUN dotnet restore "API/Inventory.API.csproj"

COPY . .
RUN dotnet publish "API/Inventory.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

RUN addgroup --system appgroup \
 && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Inventory.API.dll"]

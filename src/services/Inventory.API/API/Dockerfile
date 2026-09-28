FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:78235e09001f52b6592c458ac010775ebac6725422e80cd0c1650590f67b2743 AS build
WORKDIR /src
COPY Directory.Build.props ./

COPY ["Domain/Inventory.Domain.csproj", "Domain/packages.lock.json", "Domain/"]
COPY ["Application/Inventory.Application.csproj", "Application/packages.lock.json", "Application/"]
COPY ["Infrastructure/Inventory.Infrastructure.csproj", "Infrastructure/packages.lock.json", "Infrastructure/"]
COPY ["API/Inventory.API.csproj", "API/packages.lock.json", "API/"]

RUN dotnet restore "API/Inventory.API.csproj" --locked-mode

COPY . .
RUN dotnet publish "API/Inventory.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0.31-noble-chiseled-extra@sha256:13df490c444d723d2a6bf1aeab00f599fcd884dd66df909cbf3cd593aa1766ad AS runtime
WORKDIR /app

COPY --from=build --chown=1654:1654 /app/publish .
USER 1654:1654
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Inventory.API.dll"]

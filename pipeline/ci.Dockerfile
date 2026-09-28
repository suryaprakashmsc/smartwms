FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:78235e09001f52b6592c458ac010775ebac6725422e80cd0c1650590f67b2743 AS backend-tests
WORKDIR /src
COPY src/services/Inventory.API/ src/services/Inventory.API/
COPY tests/Inventory.Tests/ tests/Inventory.Tests/
RUN dotnet restore tests/Inventory.Tests/Inventory.Tests.csproj --locked-mode -p:NuGetAuditMode=all -p:WarningsAsErrors=NU1900%3BNU1901%3BNU1902%3BNU1903%3BNU1904
RUN dotnet build tests/Inventory.Tests/Inventory.Tests.csproj -c Release --no-restore
CMD ["dotnet", "test", "tests/Inventory.Tests/Inventory.Tests.csproj", "-c", "Release", "--no-build", "--logger", "trx;LogFileName=inventory.trx", "--results-directory", "/reports"]

FROM node:24-alpine@sha256:ebfe2f90462722a7a4de65e91990e97fe0d401c70e0e762c5b53302f905ec1c1 AS frontend-tests
WORKDIR /app
RUN npm install -g pnpm@10.16.1
COPY src/frontend/smartwms-ui/package.json src/frontend/smartwms-ui/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/frontend/smartwms-ui/ ./
RUN pnpm typecheck && pnpm lint && pnpm audit --audit-level high
CMD ["pnpm", "test:ci"]

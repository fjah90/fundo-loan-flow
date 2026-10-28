FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src

# Copy only restore inputs first so the restore layer is cached
COPY Directory.Build.props ./
COPY src/LoanFlow.Domain/LoanFlow.Domain.csproj src/LoanFlow.Domain/packages.lock.json src/LoanFlow.Domain/
COPY src/LoanFlow.Application/LoanFlow.Application.csproj src/LoanFlow.Application/packages.lock.json src/LoanFlow.Application/
COPY src/LoanFlow.Infrastructure/LoanFlow.Infrastructure.csproj src/LoanFlow.Infrastructure/packages.lock.json src/LoanFlow.Infrastructure/
COPY src/LoanFlow.Api/LoanFlow.Api.csproj src/LoanFlow.Api/packages.lock.json src/LoanFlow.Api/
RUN dotnet restore src/LoanFlow.Api/LoanFlow.Api.csproj --locked-mode

COPY src/ src/
RUN dotnet publish src/LoanFlow.Api/LoanFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble-chiseled
WORKDIR /app
COPY --from=build /app .
# Image default port 8080 and non-root user come from the chiseled base
ENTRYPOINT ["dotnet", "LoanFlow.Api.dll"]

# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first, using only the project files, so this layer is cached until dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/SpendSmart.Domain/SpendSmart.Domain.csproj src/SpendSmart.Domain/
COPY src/SpendSmart.Application/SpendSmart.Application.csproj src/SpendSmart.Application/
COPY src/SpendSmart.Infrastructure/SpendSmart.Infrastructure.csproj src/SpendSmart.Infrastructure/
COPY src/SpendSmart.Api/SpendSmart.Api.csproj src/SpendSmart.Api/
COPY src/SpendSmart.Web/SpendSmart.Web.csproj src/SpendSmart.Web/
RUN dotnet restore src/SpendSmart.Web/SpendSmart.Web.csproj

COPY src/ src/
RUN dotnet publish src/SpendSmart.Web/SpendSmart.Web.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app ./
# The official images ship a non-root "app" user.
USER app
ENTRYPOINT ["dotnet", "SpendSmart.Web.dll"]

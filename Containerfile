# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

COPY src/ ./
RUN dotnet publish RxLinkApi/RxLinkApi.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runner
WORKDIR /app

COPY --from=builder /app/publish .

# Secrets must be supplied at runtime via environment variables.
# ASP.NET Core maps env vars to config using __ as the section separator.
# Examples:
#   ConnectionStrings__DefaultConnection=Host=...
#   JwtSettings__SecretKey=...
#   Encryption__MasterKey=...
#   DeterministicEncryption__MasterKey=...
#   DeterministicEncryption__IvGenerationKey=...
#   AllowedOrigins__0=https://app.example.com

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "RxLinkApi.dll"]

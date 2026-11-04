# 1. Build the frontend
FROM node:22-alpine AS web
WORKDIR /web
COPY risk-web/package*.json ./
RUN npm ci
COPY risk-web/ ./
RUN npm run build

# 2. Build the API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY Risk.Api/Risk.Api.csproj Risk.Api/
RUN dotnet restore Risk.Api/Risk.Api.csproj
COPY Risk.Api/ Risk.Api/
RUN dotnet publish Risk.Api/Risk.Api.csproj -c Release -o /out --no-restore

# 3. Run: the API, with the built frontend in wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /out ./
COPY --from=web /web/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
# The hosting platform's proxy sends the real client address and scheme. Trusting it makes rate limits per user.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "Risk.Api.dll"]
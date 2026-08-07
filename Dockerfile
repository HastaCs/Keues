# ---------- Build del Dashboard (vite) ----------
FROM node:22-alpine AS dashboard
WORKDIR /src
COPY Keues.Dashboard/package.json Keues.Dashboard/pnpm-lock.yaml Keues.Dashboard/pnpm-workspace.yaml ./
RUN corepack enable && corepack prepare pnpm@11.13.1 --activate
RUN pnpm install --frozen-lockfile
COPY Keues.Dashboard/ .
RUN pnpm build

# ---------- Publicacion de la API .NET ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY Keues.API/ ./Keues.API/
COPY Keues.Application/ ./Keues.Application/
COPY Keues.Domain/ ./Keues.Domain/
COPY Keues.Infrastructure/ ./Keues.Infrastructure/
COPY --from=dashboard /src/dist ./Keues.API/wwwroot/
RUN dotnet publish Keues.API/Keues.API.csproj -c Release -o /app/publish --nologo

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
LABEL org.opencontainers.image.title="Keues"
LABEL org.opencontainers.image.description="Queue & ticket management system: Dashboard (React) + REST API (.NET) in a single container"
LABEL org.opencontainers.image.url="https://www.keues.dev"
LABEL org.opencontainers.image.documentation="https://www.keues.dev"
LABEL org.opencontainers.image.vendor="gorerecord"
LABEL org.opencontainers.image.licenses="Proprietary"
EXPOSE 8080
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "Keues.API.dll"]

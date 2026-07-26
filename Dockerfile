FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KromicStore.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/KromicStore.API/KromicStore.API.csproj", "src/KromicStore.API/"]
COPY ["src/KromicStore.Domain/KromicStore.Domain.csproj", "src/KromicStore.Domain/"]
COPY ["src/KromicStore.Application/KromicStore.Application.csproj", "src/KromicStore.Application/"]
COPY ["src/KromicStore.Infrastructure/KromicStore.Infrastructure.csproj", "src/KromicStore.Infrastructure/"]
COPY ["src/KromicStore.Contracts/KromicStore.Contracts.csproj", "src/KromicStore.Contracts/"]
RUN dotnet restore "src/KromicStore.API/KromicStore.API.csproj"
COPY . .
RUN dotnet publish "src/KromicStore.API/KromicStore.API.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ["scripts/entrypoint.sh", "./entrypoint.sh"]
RUN chmod +x ./entrypoint.sh
ENTRYPOINT ["./entrypoint.sh"]

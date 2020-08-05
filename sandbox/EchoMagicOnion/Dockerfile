#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 12346

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["sandbox/EchoMagicOnion/EchoMagicOnion.csproj", "sandbox/EchoMagicOnion/"]
COPY ["sandbox/EchoMagicOnion.Shared/EchoMagicOnion.Shared.csproj", "sandbox/EchoMagicOnion.Shared/"]
RUN dotnet restore "sandbox/EchoMagicOnion/EchoMagicOnion.csproj"
COPY . .
WORKDIR "/src/sandbox/EchoMagicOnion"
RUN dotnet build "EchoMagicOnion.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EchoMagicOnion.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EchoMagicOnion.dll"]
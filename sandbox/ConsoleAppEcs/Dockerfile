#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["sandbox/ConsoleAppEcs/ConsoleAppEcs.csproj", "sandbox/ConsoleAppEcs/"]
COPY ["sandbox/EchoMagicOnion.Shared/EchoMagicOnion.Shared.csproj", "sandbox/EchoMagicOnion.Shared/"]
COPY ["src/DFrame.Ecs/DFrame.Ecs.csproj", "src/DFrame.Ecs/"]
COPY ["src/DFrame/DFrame.csproj", "src/DFrame/"]
RUN dotnet restore "sandbox/ConsoleAppEcs/ConsoleAppEcs.csproj"
COPY . .
WORKDIR "/src/sandbox/ConsoleAppEcs"
RUN dotnet build "ConsoleAppEcs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppEcs.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppEcs.dll"]
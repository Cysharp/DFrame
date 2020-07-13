#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["sandbox/ConsoleApp/ConsoleApp.csproj", "sandbox/ConsoleApp/"]
COPY ["src/DFrame.ProcessWorker/DFrame.ProcessWorker.csproj", "src/DFrame.ProcessWorker/"]
COPY ["src/DFrame.Core/DFrame.Core.csproj", "src/DFrame.Core/"]
RUN dotnet restore "sandbox/ConsoleApp/ConsoleApp.csproj"
COPY . .
WORKDIR "/src/sandbox/ConsoleApp"
RUN dotnet build "ConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleApp.dll"]
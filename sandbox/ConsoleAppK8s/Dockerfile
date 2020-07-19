#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["sandbox/ConsoleAppK8s/ConsoleAppK8s.csproj", "sandbox/ConsoleAppK8s/"]
COPY ["src/DFrame.Kubernetes/DFrame.Kubernetes.csproj", "src/DFrame.Kubernetes/"]
COPY ["src/DFrame/DFrame.csproj", "src/DFrame/"]
RUN dotnet restore "sandbox/ConsoleAppK8s/ConsoleAppK8s.csproj"
COPY . .
WORKDIR "/src/sandbox/ConsoleAppK8s"
RUN dotnet build "ConsoleAppK8s.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppK8s.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppK8s.dll"]
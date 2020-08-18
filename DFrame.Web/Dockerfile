#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DFrame.Web/DFrame.Web.csproj", "DFrame.Web/"]
RUN dotnet restore "DFrame.Web/DFrame.Web.csproj"
COPY . .
WORKDIR "/src/DFrame.Web"
RUN dotnet build "DFrame.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DFrame.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DFrame.Web.dll"]
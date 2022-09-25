#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ChatWeb/ChatWeb.csproj", "ChatWeb/"]
RUN dotnet restore "ChatWeb/ChatWeb.csproj"
COPY . .
WORKDIR "/src/ChatWeb"
RUN dotnet build "ChatWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatWeb.dll"]
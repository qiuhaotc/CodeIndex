#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CodeIndex.Server/CodeIndex.Server.csproj", "CodeIndex.Server/"]
COPY ["CodeIndex.Common/CodeIndex.Common.csproj", "CodeIndex.Common/"]
COPY ["CodeIndex.MaintainIndex/CodeIndex.MaintainIndex.csproj", "CodeIndex.MaintainIndex/"]
COPY ["CodeIndex.Files/CodeIndex.Files.csproj", "CodeIndex.Files/"]
COPY ["CodeIndex.IndexBuilder/CodeIndex.IndexBuilder.csproj", "CodeIndex.IndexBuilder/"]
COPY ["CodeIndex.Search/CodeIndex.Search.csproj", "CodeIndex.Search/"]
RUN dotnet restore "CodeIndex.Server/CodeIndex.Server.csproj"
COPY . .
WORKDIR "/src/CodeIndex.Server"
RUN dotnet build "CodeIndex.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeIndex.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeIndex.Server.dll"]
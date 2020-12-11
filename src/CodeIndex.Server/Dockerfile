#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT Development
ENV CodeIndex__LuceneIndex /luceneindex
ENV CodeIndex__MonitorFolder /monitorfolder
ENV CodeIndex__MaxContentHighlightLength 3000000
ENV CodeIndex__OpenIDEUriFormat vscode://file/{FilePath}
ENV CodeIndex__IsInLinux true
ENV CodeIndex__LocalUrl http://localhost:80/
ENV CodeIndex__MonitorFolderRealPath=""
ENV CodeIndex__ExcludedExtensions=".DLL|.PBD"
ENV CodeIndex__ExcludedPaths="\\DEBUG\\|\\RELEASE\\|\\RELEASES\\|\\BIN\\|\\OBJ\\|\\DEBUGPUBLIC\\"
ENV CodeIndex__IncludedExtensions=".CS|.XML|.XAML|.JS|.TXT"
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["CodeIndex.Server/CodeIndex.Server.csproj", "CodeIndex.Server/"]
COPY ["CodeIndex.Common/CodeIndex.Common.csproj", "CodeIndex.Common/"]
COPY ["CodeIndex.Search/CodeIndex.Search.csproj", "CodeIndex.Search/"]
COPY ["CodeIndex.IndexBuilder/CodeIndex.IndexBuilder.csproj", "CodeIndex.IndexBuilder/"]
COPY ["CodeIndex.Files/CodeIndex.Files.csproj", "CodeIndex.Files/"]
COPY ["CodeIndex.MaintainIndex/CodeIndex.MaintainIndex.csproj", "CodeIndex.MaintainIndex/"]
RUN dotnet restore "CodeIndex.Server/CodeIndex.Server.csproj"
COPY . .
WORKDIR "/src/CodeIndex.Server"
RUN dotnet build "CodeIndex.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeIndex.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeIndex.Server.dll"]
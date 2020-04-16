FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
# Application
COPY ["Moonglade.Web/Moonglade.Web.csproj", "Moonglade.Web/"]
COPY ["Moonglade.Web.Authentication/Moonglade.Web.Authentication.csproj", "Moonglade.Web.Authentication/"]
COPY ["Moonglade.Web.SiteIconGenerator/Moonglade.Web.SiteIconGenerator.csproj", "Moonglade.Web.SiteIconGenerator/"]
COPY ["Moonglade.Web.Middleware/Moonglade.Web.Middleware.csproj", "Moonglade.Web.Middleware/"]
# Common
COPY ["Moonglade.DateTimeOps/Moonglade.DateTimeOps.csproj", "Moonglade.DateTimeOps/"]
COPY ["Moonglade.HtmlEncoding/Moonglade.HtmlEncoding.csproj", "Moonglade.HtmlEncoding/"]
COPY ["Moonglade.Model/Moonglade.Model.csproj", "Moonglade.Model/"]
COPY ["Moonglade.OpmlFileWriter/Moonglade.OpmlFileWriter.csproj", "Moonglade.OpmlFileWriter/"]
COPY ["Moonglade.Pingback/Moonglade.Pingback.csproj", "Moonglade.Pingback/"]
COPY ["Moonglade.Syndication/Moonglade.Syndication.csproj", "Moonglade.Syndication/"]
# Core
COPY ["Moonglade.Auditing/Moonglade.Auditing.csproj", "Moonglade.Auditing/"]
COPY ["Moonglade.Configuration/Moonglade.Configuration.csproj", "Moonglade.Configuration/"]
COPY ["Moonglade.Core/Moonglade.Core.csproj", "Moonglade.Core/"]
# Data
COPY ["Moonglade.Data/Moonglade.Data.csproj", "Moonglade.Data/"]
COPY ["Moonglade.Data.Entities/Moonglade.Data.Entities.csproj", "Moonglade.Data.Entities/"]
COPY ["Moonglade.Setup/Moonglade.Setup.csproj", "Moonglade.Setup/"]
# ImageStorage
COPY ["Moonglade.ImageStorage/Moonglade.ImageStorage.csproj", "Moonglade.ImageStorage/"]
COPY ["Moonglade.ImageStorage.AzureBlob/Moonglade.ImageStorage.AzureBlob.csproj", "Moonglade.ImageStorage.AzureBlob/"]
COPY ["Moonglade.ImageStorage.FileSystem/Moonglade.ImageStorage.FileSystem.csproj", "Moonglade.ImageStorage.FileSystem/"]
COPY ["Moonglade.Tests/Moonglade.Tests.csproj", "Moonglade.Tests/"]

RUN dotnet restore "Moonglade.Web/Moonglade.Web.csproj"
COPY . .
WORKDIR "/src/Moonglade.Web"
RUN dotnet build "Moonglade.Web.csproj" -p:Version=10.3.0-docker -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Moonglade.Web.csproj" -p:Version=10.3.0-docker -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Moonglade.Web.dll"]
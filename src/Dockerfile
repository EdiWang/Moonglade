FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Moonglade.Web/Moonglade.Web.csproj", "Moonglade.Web/"]
COPY ["Moonglade.Web.Authentication/Moonglade.Web.Authentication.csproj", "Moonglade.Web.Authentication/"]
COPY ["Moonglade.Web.FaviconGenerator/Moonglade.Web.FaviconGenerator.csproj", "Moonglade.Web.FaviconGenerator/"]
COPY ["Moonglade.Web.Middleware/Moonglade.Web.Middleware.csproj", "Moonglade.Web.Middleware/"]
# TODO: Copy rest of the projects
RUN dotnet restore "Moonglade.Web/Moonglade.Web.csproj"
COPY . .
WORKDIR "/src/Moonglade.Web"
RUN dotnet build "Moonglade.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Moonglade.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Moonglade.Web.dll"]
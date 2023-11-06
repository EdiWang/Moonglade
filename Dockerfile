FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
LABEL maintainer="edi.wang@outlook.com"
LABEL repo="https://github.com/EdiWang/Moonglade"

# If use aspnet:8.0-alpine, see https://github.com/dotnet/dotnet-docker/issues/1366
#RUN apk add --no-cache tzdata

# Captcha font
COPY ./build/OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf

WORKDIR /app
#EXPOSE 80
# https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port
# Breaking changes: Default ASP.NET Core port changed from 80 to 8080
EXPOSE 8080
EXPOSE 443

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
#ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Auto copy to prevent 996
COPY ./src/**/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

RUN dotnet restore "Moonglade.Web/Moonglade.Web.csproj"
COPY ./src .
WORKDIR "/src/Moonglade.Web"
RUN dotnet build "Moonglade.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Moonglade.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Moonglade.Web.dll"]
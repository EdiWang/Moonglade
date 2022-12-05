FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

# If use aspnet:7.0-alpine, see https://github.com/dotnet/dotnet-docker/issues/1366
#RUN apk add --no-cache tzdata

# Captcha font
COPY ./build/OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf

WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
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
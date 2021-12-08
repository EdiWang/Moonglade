FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# Captcha font
COPY ./OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf

WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
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
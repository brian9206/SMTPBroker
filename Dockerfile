FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 25

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/SMTPBroker.csproj", "./"]
RUN dotnet restore "SMTPBroker.csproj"
COPY src/ .
WORKDIR "/src/"
RUN dotnet build "SMTPBroker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SMTPBroker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY docker-entrypoint.sh .

ENV DataDir=/data
ENV ForwarderConfig=/forwarder.yml
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/ssl/server.pfx

RUN apt-get update && apt-get install -y openssl && chmod +x docker-entrypoint.sh

CMD ["/app/docker-entrypoint.sh"]

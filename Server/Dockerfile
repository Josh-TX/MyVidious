FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build

ARG TARGETARCH
WORKDIR /source
COPY MyVidious.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -a $TARGETARCH -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=dotnet-build /app .
COPY Sql/. Sql/.
COPY wwwroot/. wwwroot/.

ENTRYPOINT ["./MyVidious"]

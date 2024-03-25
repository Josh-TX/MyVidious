# Stage 1 - Build Angular
FROM node:18 AS angular-build

WORKDIR /app
copy UI/package.json ./
RUN npm install
COPY UI .
RUN npm run build


# Stage 2 - Publish dotnet 
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build

ARG TARGETARCH
WORKDIR /source
COPY Server/. .
COPY --from=angular-build /Server/wwwroot/ wwwroot
RUN dotnet publish -a $TARGETARCH -o /app


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=dotnet-build /app .
COPY Server/Sql/. Sql/.

ENTRYPOINT ["./MyVidious"]

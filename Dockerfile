FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Networking tools for debugging
RUN apt-get update && apt-get install -y iputils-ping netcat-traditional postgresql-client

COPY --from=build /out .

# neccessary for Gcloud healthchecks
EXPOSE 8080 
ENTRYPOINT ["dotnet", "AuralyticsContainer.dll"]

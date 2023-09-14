#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CodelistProvider/CodelistProvider.csproj", "CodelistProvider/"]
COPY ["Abstractions/Abstractions.csproj", "Abstractions/"]
COPY ["DocumentStorageClient/DocumentStorageClient.csproj", "DocumentStorageClient/"]
COPY ["RdfFileStorage.Test/RdfFileStorage.Test.csproj", "RdfFileStorage.Test/"]
COPY ["RdfFileStorage/RdfFileStorage.csproj", "RdfFileStorage/"]
RUN dotnet restore "CodelistProvider/CodelistProvider.csproj"
COPY . .
WORKDIR "/src/CodelistProvider"
RUN dotnet build "CodelistProvider.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodelistProvider.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodelistProvider.dll"]
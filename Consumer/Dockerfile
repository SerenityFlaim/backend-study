
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Consumer/Consumer.csproj Consumer/
COPY Models/Models.csproj Models/
COPY Project.Common/Project.Common.csproj Project.Common/
COPY Project.Messages/Project.Messages.csproj Project.Messages/
RUN dotnet restore Consumer/Consumer.csproj

COPY Consumer/ Consumer/
COPY Project.Common/ Project.Common/
COPY Models/ Models/
COPY Project.Messages/ Project.Messages/

RUN dotnet publish Consumer/Consumer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Consumer.dll"]  
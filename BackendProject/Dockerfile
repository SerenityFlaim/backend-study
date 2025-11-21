FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /

COPY BackendProject/BackendProject.csproj BackendProject/
COPY Migrations/Migrations.csproj Migrations/
COPY Models/Models.csproj Models/
COPY Project.Common/Project.Common.csproj Project.Common/
COPY Project.Messages/Project.Messages.csproj Project.Messages/
COPY Consumer/Consumer.csproj Consumer/
RUN dotnet restore "BackendProject/BackendProject.csproj"

COPY . .

RUN dotnet publish "WebApi/WebApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BackendProject.dll"]
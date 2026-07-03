FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Worker/Worker.csproj", "src/Worker/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
RUN dotnet restore "src/Worker/Worker.csproj"

COPY ["src/Worker/", "src/Worker/"]
COPY ["src/Application/", "src/Application/"]
COPY ["src/Infrastructure/", "src/Infrastructure/"]
COPY ["src/Domain/", "src/Domain/"]
RUN dotnet publish "src/Worker/Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Worker.dll"]

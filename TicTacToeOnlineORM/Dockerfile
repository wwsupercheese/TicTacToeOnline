FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicTacToeOnlineORM/TicTacToeOnlineORM.csproj", "TicTacToeOnlineORM/"]
RUN dotnet restore "TicTacToeOnlineORM/TicTacToeOnlineORM.csproj"
COPY . .
WORKDIR "/src/TicTacToeOnlineORM"
RUN dotnet build "TicTacToeOnlineORM.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicTacToeOnlineORM.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicTacToeOnlineORM.dll"]
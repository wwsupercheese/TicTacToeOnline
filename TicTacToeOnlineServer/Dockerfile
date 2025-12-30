FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Копируем проект сервера и ORM (так как сервер может зависеть от типов ORM/Protos)
COPY ["TicTacToeOnlineServer/TicTacToeOnlineServer.csproj", "TicTacToeOnlineServer/"]
COPY ["TicTacToeOnlineORM/TicTacToeOnlineORM.csproj", "TicTacToeOnlineORM/"]
RUN dotnet restore "TicTacToeOnlineServer/TicTacToeOnlineServer.csproj"
COPY . .
WORKDIR "/src/TicTacToeOnlineServer"
RUN dotnet build "TicTacToeOnlineServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicTacToeOnlineServer.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicTacToeOnlineServer.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ExpenseManager.Domain/ExpenseManager.Domain.csproj", "src/ExpenseManager.Domain/"]
COPY ["src/ExpenseManager.Application/ExpenseManager.Application.csproj", "src/ExpenseManager.Application/"]
COPY ["src/ExpenseManager.Infrastructure/ExpenseManager.Infrastructure.csproj", "src/ExpenseManager.Infrastructure/"]
COPY ["src/ExpenseManager.API/ExpenseManager.API.csproj", "src/ExpenseManager.API/"]
RUN dotnet restore "src/ExpenseManager.API/ExpenseManager.API.csproj"

COPY src/ src/
RUN dotnet publish "src/ExpenseManager.API/ExpenseManager.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ExpenseManager.API.dll"]

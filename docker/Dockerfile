# Dockerfile cho ATS.Api — TV1 phụ trách, hoàn thiện ở tuần 8.
# Chạy từ thư mục gốc repo:  docker build -f docker/Dockerfile -t ats-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file project trước để tận dụng cache của Docker
COPY src/ATS.Contracts/*.csproj ./ATS.Contracts/
COPY src/ATS.Data/*.csproj      ./ATS.Data/
COPY src/ATS.Business/*.csproj  ./ATS.Business/
COPY src/ATS.AI/*.csproj        ./ATS.AI/
COPY src/ATS.Api/*.csproj       ./ATS.Api/
RUN dotnet restore ./ATS.Api/ATS.Api.csproj

COPY src/ ./
RUN dotnet publish ./ATS.Api/ATS.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ATS.Api.dll"]

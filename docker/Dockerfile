# Dockerfile dùng chung cho ATS.Api, ATS.Web và worker ATS.AI — TV1 phụ trách, hoàn thiện tuần 8.
# Chọn project bằng build arg PROJECT (xem docker-compose.yml).
#
# Chạy tay từ thư mục gốc repo:
#   docker build -f docker/Dockerfile --build-arg PROJECT=ATS.Api -t ats-api .
#   docker build -f docker/Dockerfile --build-arg PROJECT=ATS.AI  -t ats-worker .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT=ATS.Api
WORKDIR /src

# Copy file project trước để tận dụng cache của Docker: chỉ sửa code thì không phải restore lại.
COPY src/ATS.Contracts/*.csproj ./ATS.Contracts/
COPY src/ATS.Data/*.csproj      ./ATS.Data/
COPY src/ATS.Business/*.csproj  ./ATS.Business/
COPY src/ATS.AI/*.csproj        ./ATS.AI/
COPY src/ATS.Api/*.csproj       ./ATS.Api/
COPY src/ATS.Web/*.csproj       ./ATS.Web/
RUN dotnet restore ./${PROJECT}/${PROJECT}.csproj

COPY src/ ./
RUN dotnet publish ./${PROJECT}/${PROJECT}.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG PROJECT=ATS.Api
WORKDIR /app
COPY --from=build /app/publish .

# Tên dll phải truyền qua biến vì Dockerfile này dùng chung cho nhiều project.
ENV APP_DLL=${PROJECT}.dll
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# `exec` để tiến trình dotnet nhận đúng PID 1 và xử lý được tín hiệu dừng của Docker
# (không có `exec` thì `docker compose down` phải chờ hết timeout mới kill).
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]

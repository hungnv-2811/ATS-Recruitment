# Dockerfile dùng chung cho ATS.Api, ATS.Web và ATS.Worker — TV1 phụ trách, hoàn thiện tuần 2.
# Chọn project bằng build arg PROJECT_PATH + PROJECT_DLL (xem docker-compose.yml).
#
# Chạy tay từ thư mục gốc repo:
#   docker build -f docker/Dockerfile \
#     --build-arg PROJECT_PATH=src/Hosts/ATS.Api \
#     --build-arg PROJECT_DLL=ATS.Api.dll \
#     -t ats-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT_PATH=src/Hosts/ATS.Api
WORKDIR /src

# Copy solution + toàn bộ src (Modular Monolith có 18 project — copy nguyên cây gọn hơn).
# Hai file props ở thư mục gốc PHẢI copy cùng:
#   Directory.Build.props    — TargetFramework dùng chung
#   Directory.Packages.props — version của mọi package (Central Package Management)
# Thiếu file thứ hai thì .csproj không có Version nào cả, NuGet tụt về bản thấp nhất
# (Npgsql 1.0.0 → npgsql 3.1.5 có lỗ hổng bảo mật) và restore đỏ.
COPY ATS.sln Directory.Build.props Directory.Packages.props ./
COPY src/ ./src/

RUN dotnet restore ./ATS.sln
RUN dotnet publish ./${PROJECT_PATH}/*.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG PROJECT_DLL=ATS.Api.dll
WORKDIR /app
COPY --from=build /app/publish .

ENV APP_DLL=${PROJECT_DLL}
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# `exec` để tiến trình dotnet nhận PID 1 và xử lý tín hiệu dừng đúng cách.
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]

# Dockerfile dùng chung cho ATS.Api, ATS.Web và ATS.Worker — TV1 phụ trách, hoàn thiện tuần 2.
# Chọn project bằng build arg PROJECT_PATH + PROJECT_DLL (xem docker-compose.yml).
#
# Chạy tay từ thư mục gốc repo:
#   docker build -f docker/Dockerfile \
#     --build-arg PROJECT_PATH=src/Hosts/ATS.Api \
#     --build-arg PROJECT_DLL=ATS.Api.dll \
#     -t ats-api .

# Phien ban .NET: doi o day va trong global.json. Directory.Build.props giu
# TargetFramework cho code; ba cho do phai khop nhau.
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG PROJECT_PATH=src/Hosts/ATS.Api
WORKDIR /src

# Copy solution + toàn bộ src (Modular Monolith có 18 project — copy nguyên cây gọn hơn).
# Ba file ở thư mục gốc PHẢI copy cùng:
#   Directory.Build.props    — TargetFramework dùng chung
#   Directory.Packages.props — version của mọi package (Central Package Management)
#   global.json              — ghim feature band của SDK
# Thiếu Directory.Packages.props thì .csproj không có Version nào cả, NuGet tụt về
# bản thấp nhất (Npgsql 1.0.0 → npgsql 3.1.5 có lỗ hổng bảo mật) và restore đỏ.
COPY ATS.sln Directory.Build.props Directory.Packages.props global.json ./

# Copy RIÊNG các .csproj trước, restore, rồi mới copy mã nguồn.
#
# Nếu copy cả src/ trước khi restore thì sửa MỘT dòng C# là invalidate layer
# restore, và toàn bộ NuGet graph của 18 project bị tải lại — nhân 3 vì compose
# build 3 image. Tách ra thì sửa code chỉ phá layer cuối, và cả 3 image dùng
# chung một layer restore đã cache.
#
# Thêm project mới thì thêm một dòng ở đây, nếu không restore sẽ báo thiếu.
COPY src/Hosts/ATS.Api/ATS.Api.csproj src/Hosts/ATS.Api/
COPY src/Hosts/ATS.Web/ATS.Web.csproj src/Hosts/ATS.Web/
COPY src/Hosts/ATS.Worker/ATS.Worker.csproj src/Hosts/ATS.Worker/
COPY src/Modules/AiScreening/ATS.AiScreening.Application/ATS.AiScreening.Application.csproj src/Modules/AiScreening/ATS.AiScreening.Application/
COPY src/Modules/AiScreening/ATS.AiScreening.Domain/ATS.AiScreening.Domain.csproj src/Modules/AiScreening/ATS.AiScreening.Domain/
COPY src/Modules/AiScreening/ATS.AiScreening.Infrastructure/ATS.AiScreening.Infrastructure.csproj src/Modules/AiScreening/ATS.AiScreening.Infrastructure/
COPY src/Modules/Identity/ATS.Identity.Application/ATS.Identity.Application.csproj src/Modules/Identity/ATS.Identity.Application/
COPY src/Modules/Identity/ATS.Identity.Domain/ATS.Identity.Domain.csproj src/Modules/Identity/ATS.Identity.Domain/
COPY src/Modules/Identity/ATS.Identity.Infrastructure/ATS.Identity.Infrastructure.csproj src/Modules/Identity/ATS.Identity.Infrastructure/
COPY src/Modules/Recruitment/ATS.Recruitment.Application/ATS.Recruitment.Application.csproj src/Modules/Recruitment/ATS.Recruitment.Application/
COPY src/Modules/Recruitment/ATS.Recruitment.Domain/ATS.Recruitment.Domain.csproj src/Modules/Recruitment/ATS.Recruitment.Domain/
COPY src/Modules/Recruitment/ATS.Recruitment.Infrastructure/ATS.Recruitment.Infrastructure.csproj src/Modules/Recruitment/ATS.Recruitment.Infrastructure/
COPY src/Shared/ATS.Persistence/ATS.Persistence.csproj src/Shared/ATS.Persistence/
COPY src/Shared/ATS.SharedKernel/ATS.SharedKernel.csproj src/Shared/ATS.SharedKernel/
COPY src/Tests/ATS.AiScreening.Tests/ATS.AiScreening.Tests.csproj src/Tests/ATS.AiScreening.Tests/
COPY src/Tests/ATS.ArchitectureTests/ATS.ArchitectureTests.csproj src/Tests/ATS.ArchitectureTests/
COPY src/Tests/ATS.IntegrationTests/ATS.IntegrationTests.csproj src/Tests/ATS.IntegrationTests/
COPY src/Tests/ATS.Recruitment.Tests/ATS.Recruitment.Tests.csproj src/Tests/ATS.Recruitment.Tests/

RUN dotnet restore ./ATS.sln

COPY src/ ./src/

# --no-restore: dòng trên đã restore cả solution rồi, không cần làm lại lần hai.
RUN dotnet publish ./${PROJECT_PATH}/*.csproj -c Release -o /app/publish     --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
ARG PROJECT_DLL=ATS.Api.dll
WORKDIR /app
COPY --from=build /app/publish .

ENV APP_DLL=${PROJECT_DLL}
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# `exec` để tiến trình dotnet nhận PID 1 và xử lý tín hiệu dừng đúng cách.
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]

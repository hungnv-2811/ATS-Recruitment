# ATS.Recruitment.Application

Use case của module tuyển dụng: `JobService`, `CvService`, `ApplicationService`, `InterviewService`.

Layered đơn giản (không CQRS đầy đủ) vì đây là CRUD. Chỉ AiScreening dùng CQRS.

Cross-module port đặt trong `Ports/IApplicationScreeningTrigger.cs` — AiScreening.Infrastructure implement.

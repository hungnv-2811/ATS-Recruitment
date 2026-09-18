# ATS.ArchitectureTests

NetArchTest — ép ranh giới kiến trúc, CI đỏ nếu vi phạm.

## 3 rule bắt buộc

1. **Domain không reference Infrastructure**
   ```csharp
   Types.InAssembly(DomainAssembly)
        .ShouldNot()
        .HaveDependencyOn("ATS.*.Infrastructure")
   ```

2. **Port AI chỉ nhận `AnonymizedCv`**
   ```csharp
   Types.InNamespace("ATS.AiScreening.Domain.Ports")
        .That().AreInterfaces()
        .Should()
        .NotHaveMethodsWithParameter<string>("cv")
        .And().NotHaveMethodsWithParameter<Cv>()
   ```

3. **Controllers không đụng `DbContext`**
   ```csharp
   Types.InAssembly(ApiAssembly)
        .That().AreClasses().And().HaveNameEndingWith("Controller")
        .ShouldNot()
        .HaveDependencyOn("Microsoft.EntityFrameworkCore.DbContext")
   ```

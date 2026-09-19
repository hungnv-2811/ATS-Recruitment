using ATS.Recruitment.Domain;
using Xunit;

namespace ATS.Recruitment.Tests;

public sealed class ApplicationStatusTests
{
    [Fact]
    public void Co_du_nam_trang_thai_nghiep_vu()
    {
        // Chuoi trang thai nghiep vu: Applied -> Screening -> Interview -> Hired,
        // re nhanh sang Rejected. Xem docs/use-cases.md muc 4.
        Assert.Equal(5, Enum.GetValues<ApplicationStatus>().Length);
    }

    [Fact]
    public void Trang_thai_ung_tuyen_tach_roi_trang_thai_cham_AI()
    {
        // RB9: hai chuoi trang thai DOC LAP. Neu mot ngay nao do co nguoi them
        // "Scored" hay "Failed" vao day thi nghia la hai chuoi da bi tron —
        // luc do HR se bi chan thao tac chi vi AI chua cham xong.
        var ten = Enum.GetNames<ApplicationStatus>();

        Assert.DoesNotContain("Scored", ten);
        Assert.DoesNotContain("Failed", ten);
        Assert.DoesNotContain("Processing", ten);
    }
}

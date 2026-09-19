namespace ATS.Recruitment.Domain;

/// <summary>
/// Chuoi trang thai NGHIEP VU, chi HR dieu khien.
/// DOC LAP voi trang thai cham diem AI (RB9): diem AI khong bao gio tu chuyen trang thai nay.
/// Khong duoc nhay coc tu Applied thang sang Hired.
/// </summary>
public enum ApplicationStatus
{
    Applied = 0,
    Screening = 1,
    Interview = 2,
    Hired = 3,
    Rejected = 4,
}

public enum JobStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2,
}

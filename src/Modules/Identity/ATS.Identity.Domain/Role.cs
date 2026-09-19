namespace ATS.Identity.Domain;

/// <summary>
/// Vai cua tai khoan. Candidate va HR la CHU THE NGHIEP VU;
/// Admin chi la vai ky thuat (quan ly tai khoan), khong tham gia tuyen dung.
/// </summary>
public enum Role
{
    Candidate = 0,
    Hr = 1,
    Admin = 2,
}

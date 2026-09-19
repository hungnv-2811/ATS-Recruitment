namespace ATS.AiScreening.Domain;

/// <summary>Diem phu hop 0–100. La kieu rieng, khong phai int tran.</summary>
public sealed record Score
{
    public Score(int value)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, "Diem phu hop phai nam trong khoang [0, 100].");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

namespace ServerlessAPI.Entities;

/// <summary>Scores on the 0-20 scale used in Peru. (StudentId, CourseId, Term) is the upsert key.</summary>
public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Term { get; set; } = string.Empty;

    public decimal Score1 { get; set; }
    public decimal Score2 { get; set; }
    public decimal Score3 { get; set; }
    public decimal Score4 { get; set; }
    public decimal Score5 { get; set; }
    public decimal Average { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

namespace DataAccessProviderConsole.Models;

public class Diary
{
    public int DiaryID { get; set; }
    public string UserID { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Content { get; set; } = string.Empty;
}
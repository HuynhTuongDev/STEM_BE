namespace STEM.Application.Dtos.LoginHistory;

public class GetLoginHistoriesRequest
{
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IScheduleRepository _scheduleRepository;

    public RoomsController(IScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRooms(CancellationToken cancellationToken = default)
    {
        try
        {
            var rooms = await _scheduleRepository.GetAllRoomsAsync(cancellationToken);
            return Ok(new { success = true, data = rooms });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách phòng.", error = ex.Message });
        }
    }
}

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;


    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }


    /// <summary>
    /// Получить бронирование по ID
    /// </summary>
    /// <param name="id"> Id бронирования</param>
     // GET: api/bookings
     [HttpGet("{id}")]
     public async Task<IActionResult> GetById(Guid id)
    {
       var booking = await _bookingService.GetBookingByIdAsync(id);
        return Ok(MapToResponse(booking));
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    [HttpPost("{id}")]
    public async Task<IActionResult> Add(){
        return Ok();
    }

    private static BookingResponse MapToResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
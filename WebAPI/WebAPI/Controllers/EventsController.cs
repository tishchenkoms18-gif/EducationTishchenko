using Microsoft.AspNetCore.Mvc;
namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        IBookingService bookingService,
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список всех событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="title">Поиск по названию (частичное совпадение, регистронезависимый)</param>
    /// <param name="from">События, начинающиеся не раньше указанной даты</param>
    /// <param name="to">События, заканчивающиеся не позже указанной даты</param>
    /// <param name="page">Страница, которую необходимо вернуть (по умолчанию 1)</param>
    /// <param name="pageSize">Количество элементов на странице (по умолчанию 10, макс. 100)</param>
    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var events = await _eventService.GetAllEventAsync(title, from, to, page, pageSize);

        var response = new PaginatedResult<EventResponse>
        {
            TotalCount = events.TotalCount,
            Items = events.Items.Select(MapToResponse).ToList(),
            PageNumber = events.PageNumber,
            PageSize = events.PageSize,
            TotalPages = events.TotalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    // GET: api/events/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var eventEntity = await _eventService.GetByIdEventAsync(id);
        return Ok(MapToResponse(eventEntity));
    }

    /// <summary>
    /// Создать бронирование на событие (быстрый ответ 202 Accepted)
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <returns>202 Accepted + Location</returns>
    [HttpPost("{id}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookEventAsync(Guid id)
    {
        // Создаём бронь (исключения уходят в middleware)
        var booking = await _bookingService.CreateBookingAsync(id);

        var response = new BookingResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        return AcceptedAtAction(
            actionName: nameof(BookingsController.GetById),
            controllerName: "Bookings",
            routeValues: new { id = booking.Id },
            value: response);
    }

    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="request">Данные для создания события</param>
    // POST: api/events
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var eventEntity = await _eventService.CreateEventAsync(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        return CreatedAtAction(
            nameof(GetById),
            new { id = eventEntity.Id },
            MapToResponse(eventEntity));
    }

    /// <summary>
    /// Создание нескольких событий за один запрос
    /// </summary>
    /// <param name="request">Список событий для создания</param>
    /// <returns>Список созданных событий</returns>
    /// <response code="201">Все события успешно созданы</response>
    /// <response code="400">Ошибка валидации или превышено максимальное количество</response>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateEventsRequest request)
    {
        if (request.Events == null || !request.Events.Any())
            return BadRequest(new { error = "Список событий не может быть пустым" });

        if (request.Events.Count > 100)
            return BadRequest(new { error = "Нельзя создать более 100 событий за раз" });

       
        var createdEvents = await _eventService.CreateEventsAsync(request.Events);

        var response = new
        {
            TotalCreated = createdEvents.Count,
            Events = createdEvents.Select(MapToResponse).ToList(),
            Message = $"Успешно создано {createdEvents.Count} событий"
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Обновление события (полное)
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="request">Данные для обновления</param>
    // PUT: api/events/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var eventEntity = await _eventService.UpdateEventAsync(
            id,
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt);

        return Ok(MapToResponse(eventEntity));
    }

    /// <summary>
    /// Удаление события по ID
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    // DELETE: api/events/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteEventAsync(id);
        return NoContent();
    }

    // =============================================
    // Приватные методы
    // =============================================

    private static EventResponse MapToResponse(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartAt = eventEntity.StartAt,
            EndAt = eventEntity.EndAt,
            CreatedAt = eventEntity.CreatedAt,
            TotalSeats = eventEntity.TotalSeats,
            AvailableSeats = eventEntity.AvailableSeats
        };
    }
}
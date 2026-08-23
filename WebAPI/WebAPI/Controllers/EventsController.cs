using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }
/// <summary>
/// Получить список всех событий с фильтрацией
/// </summary>
/// <param name="title">Поиск по названию (частичное совпадение, регистронезависимый)</param>
/// <param name="from">События, начинающиеся не раньше указанной даты</param>
/// <param name="to">События, заканчивающиеся не позже указанной даты</param>
/// <param name="page">Cтраница, которую необходимо вернуть</param>
/// <param name="pageSize">Количество элементов на странице</param>
/// <returns></returns>
    // GET: api/events
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? title, [FromQuery] DateTime? from,[FromQuery] DateTime? to, int page = 1, int pageSize = 10 )
    {
        if(page < 1)
        {
            return BadRequest(new { error = "Номер страницы должен быть больше 0" });
        }

        if (pageSize < 1 || pageSize > 100)
        return BadRequest(new { error = "Размер страницы должен быть от 1 до 100" });

        var events =  _eventService.GetAllEvent(title, from, to, page, pageSize);
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
    /// Получение события по ИД
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // GET: api/events/{id}
    [HttpGet("{id}")]
    public IActionResult GetById([FromRoute] Guid id)
    {
       try
    {
        var eventEntity = _eventService.GetByIdEvent(id);
        return Ok(MapToResponse(eventEntity));
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
    }
    
    /// <summary>
/// Создать бронирование на событие (быстрый ответ 202 Accepted)
/// </summary>
/// <param name="id">Идентификатор события</param>
/// <returns>202 Accepted + Location</returns>
[HttpPost("{id}/book")]
public async Task<IActionResult> BookEventAsync(Guid id)
{
    try
    {
        var eventExists = _eventService.GetByIdEvent(id);
    }
    catch (EventNotFoundException)
    {
        return NotFound($"Событие с ID {id} не найдено");
    }

    // Создаём бронь
    var booking = await _bookingService.CreateBookingAsync(id);

    // Формируем ответ 202 Accepted с Location
    var response = new BookingResponse
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status.ToString(),
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };

    return AcceptedAtAction(
        actionName: "GetBookingById",    // имя метода в BookingsController
        controllerName: "Bookings",
        routeValues: new { id = booking.Id },
        value: response);
}
    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    // POST: api/events
    [HttpPost]
    public IActionResult Create([FromBody] CreateEventRequest request)
    {
        try
        {
            ValidateRequest(request.Title, request.StartAt, request.EndAt);

            var eventEntity = _eventService.CreateEvent(
                request.Title,
                request.Description,
                request.StartAt,
                request.EndAt);

            return CreatedAtAction(
                nameof(GetById),
                new { id = eventEntity.Id },
                MapToResponse(eventEntity));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Создание нескольких событий за один запрос
    /// </summary>
    /// <param name="request">Список событий для создания</param>
    /// <returns>Список созданных событий</returns>
    /// <response code="201">Все события успешно созданы</response>
    /// <response code="400">Ошибка валидации или превышено максимальное количество</response>
    [HttpPost("batch")]
    public IActionResult CreateBatch([FromBody] CreateEventsRequest request)
    {
        try
        {
            if (request.Events == null || !request.Events.Any())
                return BadRequest(new { error = "Список событий не может быть пустым" });

            if (request.Events.Count > 100)
                return BadRequest(new { error = "Нельзя создать более 100 событий за раз" });

            var createdEvents = _eventService.CreateEvents(request.Events);

            var response = new
            {
                TotalCreated = createdEvents.Count,
                Events = createdEvents.Select(MapToResponse).ToList(),
                Message = $"Успешно создано {createdEvents.Count} событий"
            };

            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
    /// <summary>
    /// Обновление события (полное)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    // PUT: api/events/{id}
    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        
            ValidateRequest(request.Title, request.StartAt, request.EndAt);

            var eventEntity = _eventService.UpdateEvent(
                id,
                request.Title,
                request.Description,
                request.StartAt,
                request.EndAt);

            return Ok(MapToResponse(eventEntity));
       
    }
    /// <summary>
    /// Удаление события по ИД
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // DELETE: api/events/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _eventService.DeleteEvent(id); 
        return NoContent();
    }

    
    private static void ValidateRequest(string title, DateTime startAt, DateTime endAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название события обязательно");

        if (startAt >= endAt)
            throw new ArgumentException("Дата начала должна быть раньше даты окончания");
    }

    private static EventResponse MapToResponse(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartAt = eventEntity.StartAt,
            EndAt = eventEntity.EndAt,
            CreatedAt = eventEntity.CreatedAt
        };
    }
}
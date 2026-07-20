using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }
/// <summary>
/// Получить список всех событий с фильтрацией
/// </summary>
/// <param name="title">Поиск по названию (частичное совпадение, регистронезависимый)</param>
/// <param name="from">События, начинающиеся не раньше указанной даты</param>
/// <param name="to">События, заканчивающиеся не позже указанной даты</param>
/// <returns></returns>
    // GET: api/events
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? title, [FromQuery] DateTime? from,[FromQuery] DateTime? to)
    {
        var events =  _eventService.GetAllEvent().ToList();
        return Ok(events.Select(MapToResponse));
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
        var eventEntity = _eventService.GetByIdEvent(id);
        if (eventEntity == null)
            return NotFound($"Событие с ID {id} не найдено");

        return Ok(MapToResponse(eventEntity));
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
    /// Обновление события (полное)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    // PUT: api/events/{id}
    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        try
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
        catch (KeyNotFoundException)
        {
            return NotFound($"Событие с ID {id} не найдено");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
        var result = _eventService.DeleteEvent(id);
        if (!result)
            return NotFound($"Событие с ID {id} не найдено");

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
            EndAt = eventEntity.EndAt
        };
    }
}
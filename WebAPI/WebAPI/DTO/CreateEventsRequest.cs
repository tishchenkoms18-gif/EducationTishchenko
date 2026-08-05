/// <summary>
/// Запрос на создание событий
/// </summary>
public class CreateEventsRequest
{
   public List<CreateEventRequest> Events { get; set; } = new();
}

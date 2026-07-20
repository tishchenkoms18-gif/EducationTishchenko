/// <summary>
/// Доменная модель события (мероприятия)
/// </summary>
public class Event
{
    public Guid Id { get; private set; }  
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

     public static Event Create(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Validate(title, startAt, endAt);

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt.ToUniversalTime(),
            EndAt = endAt.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Validate(title, startAt, endAt);

        Title = title;
        Description = description;
        StartAt = startAt.ToUniversalTime();
        EndAt = endAt.ToUniversalTime();
    }

    private static void Validate(string title, DateTime startAt, DateTime endAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название события обязательно");

        if (title.Length > 300)
            throw new ArgumentException("Название не должно превышать 300 символов");

        if (startAt >= endAt)
            throw new ArgumentException("Дата начала должна быть раньше даты окончания");

        if (startAt < DateTime.UtcNow.AddHours(-1))
            throw new ArgumentException("Нельзя создавать событие в прошлом");
    }
}

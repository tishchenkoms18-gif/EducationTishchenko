// WebAPI.Tests/Fixtures/TestData.cs
using WebAPI;

public static class TestData
{
    // Стандартный набор событий
    public static List<Event> GetSampleEvents(EventService service)
    {
        return new List<Event>
        {
            service.CreateEvent("C# Workshop", "Programming", 
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)),
            
            service.CreateEvent("C# Conference", "Programming", 
                DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6)),
            
            service.CreateEvent("F# Workshop", "Programming", 
                DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4)),
            
            service.CreateEvent("ASP.NET Core", "Web", 
                DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11)),
            
            service.CreateEvent("Azure DevOps", "DevOps", 
                DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(16)),

            service.CreateEvent(".NET MVC", "Web", 
                DateTime.UtcNow.AddDays(20), DateTime.UtcNow.AddDays(21))
        };
    }

    // Набор событий для пагинации (50 штук)
    public static List<Event> GetPaginationEvents(EventService service)
    {
        var events = new List<Event>();
        for (int i = 1; i <= 50; i++)
        {
            events.Add(service.CreateEvent($"Event {i}", $"Test: {i}", 
                DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i + 1)));
        }
        return events;
    }

    // Набор для тестов дат (прошлое/настоящее/будущее)
    public static List<Event> GetDateRangeEvents(EventService service)
    {
        var baseDate = DateTime.UtcNow.Date;
        return new List<Event>
        {
            
            service.CreateEvent("Current Event", "Test Current", 
                baseDate.AddDays(1), baseDate.AddDays(2)),
            
            service.CreateEvent("Future Event", "Test Future", 
                baseDate.AddDays(10), baseDate.AddDays(11)),

            service.CreateEvent("Event ",  null, 
                baseDate.AddDays(4),baseDate.AddDays(8))
        };
    }
}
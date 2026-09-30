// WebAPI.Tests/Fixtures/TestData.cs
public static class TestData
{
    public static List<Event> GetSampleEvents(EventService service)
    {
        return new List<Event>
        {
            service.CreateEventAsync("C# Workshop", "Programming",
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10).Result,

            service.CreateEventAsync("C# Conference", "Programming",
                DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6), 50).Result,

            service.CreateEventAsync("F# Workshop", "Programming",
                DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 20).Result,

            service.CreateEventAsync("ASP.NET Core", "Web",
                DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11), 30).Result,

            service.CreateEventAsync("Azure DevOps", "DevOps",
                DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(16), 40).Result,
        };
    }

    public static List<Event> GetPaginationEvents(EventService service)
    {
        var events = new List<Event>();
        for (int i = 1; i <= 50; i++)
        {
            events.Add(service.CreateEventAsync($"Event {i}", null,
                DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i + 1), 100).Result);
        }
        return events;
    }

    public static List<Event> GetDateRangeEvents(EventService service)
    {
        var baseDate = DateTime.UtcNow.Date;
        return new List<Event>
        {
            service.CreateEventAsync("Current Event", null,
                baseDate.AddDays(1), baseDate.AddDays(2), 10).Result,

            service.CreateEventAsync("Future Event", null,
                baseDate.AddDays(10), baseDate.AddDays(11), 10).Result,
        };
    }
}
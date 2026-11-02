using System;

public interface IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task AddAsync(Booking bookingEntity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(Booking bookingEntity, CancellationToken cancellationToken = default);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
    public Task<List<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    public Task<List<Booking>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<List<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default);
}

using System;
using Microsoft.EntityFrameworkCore;
using WebAPI.DataAccess.Repositories;

namespace WebAPI.DataAccess.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

   public BookingRepository(AppDbContext db )
    {
        _db = db;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
    public async Task AddAsync(Booking bookingEntity, CancellationToken cancellationToken = default)
    {
        await _db.Bookings.AddAsync(bookingEntity, cancellationToken);
    }
    public Task UpdateAsync(Booking bookingEntity, CancellationToken cancellationToken = default)
    {
         _db.Bookings.Update(bookingEntity);
        return Task.CompletedTask;
    }
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.Id == id, cancellationToken);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
         return _db.SaveChangesAsync(cancellationToken);
    }
    public async Task<List<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.EventId == eventId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task<List<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
         return await _db.Bookings
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task<List<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

}

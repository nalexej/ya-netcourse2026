using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.Infrastructure.Repositories
{
    /// <summary>
    /// Потокобезопасная реализация <see cref="IBookingRepository"/>, 
    /// хранящая события в базе данных.
    /// </summary>
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="context">Контекст БД.</param>
        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task AddAsync(Booking @booking, CancellationToken ct)
        {
            if (@booking == null)
                throw new ArgumentNullException(nameof(@booking));
            return _context.Bookings.AddAsync(@booking, ct).AsTask();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Booking>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Bookings.ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
        {
            if (eventId == Guid.Empty)
                return await Task.FromResult(Enumerable.Empty<Booking>());

            var bookings = await _context.Bookings.Where(b => b.EventId == eventId).ToListAsync(ct);
            return bookings;
        }

        /// <inheritdoc />
        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Bookings.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken ct = default)
        {
            var bookings = await _context.Bookings.Where(b => b.Status == status).ToListAsync(ct);
            return bookings;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> GetPendingBookingsIdsAsync(CancellationToken ct = default)
        {
            var bookingsIds = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Pending)
                    .Select(b => b.Id)
                    .ToListAsync(ct);
            return bookingsIds;
        }

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }
    }
}

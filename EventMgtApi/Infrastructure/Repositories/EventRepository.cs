using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.Infrastructure.Repositories
{
    /// <summary>
    /// Потокобезопасная реализация <see cref="IEventRepository"/>, 
    /// хранящая события в базе данных.
    /// </summary>
    public class EventRepository : IEventRepository
    {

        private readonly AppDbContext _context;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="context">Контекст БД.</param>
        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task AddAsync(Event @event, CancellationToken ct = default)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            return _context.Events.AddAsync(@event, ct).AsTask();
        }

        /// <inheritdoc />
        public async Task DeleteAsync(Event @event, CancellationToken ct = default)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            _context.Events.Remove(@event);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Events.ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<PaginatedResult<Event>> GetFilteredPagesAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            // Получаем все события
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var pattern = $"%{title}%";
                query = query.Where(e => EF.Functions.ILike(e.Title, pattern));
            }

            if (from.HasValue)
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }

            // Подсчитываем общее количество
            var totalCount = await query.CountAsync();

            // Пагинация
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct); // Выполняем запрос к БД

            // Формируем результат
            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        }

        /// <inheritdoc />
        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }

    }
}

using EventMgtApi.Contracts.Events.DTOs;
using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Domain.Entities;
using EventMgtApi.EventsService.Domain.Exceptions;
using EventMgtApi.EventsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.EventsService.Infrastructure.Repositories
{
    /// <summary>
    /// Потокобезопасная реализация <see cref="IEventRepository"/>, 
    /// хранящая события в базе данных.
    /// </summary>
    public class EventRepository : IEventRepository
    {

        private readonly EventDbContext _context;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="context">Контекст БД.</param>
        public EventRepository(EventDbContext context)
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
        public async Task<T> ExecuteWithConcurrencyRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            CancellationToken ct = default)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    var result = await operation();
                    await transaction.CommitAsync(ct);
                    return result;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync(ct);
                    if (attempt == maxRetries - 1)
                        throw; // последняя попытка — пробрасываем
                               // иначе — retry
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw; // любое другое исключение — сразу наружу
                }
            }

            // Все retry исчерпаны — мест больше нет (или они были заняты другими)
            throw new NoAvailableSeatsException("Нет доступных мест для данного события.");
        }

        /// <inheritdoc />
        public async Task<Event?> GetWithLockAsync(Guid id, CancellationToken ct = default)
        {
            var sql = @"SELECT ""id"", ""title"", ""description"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats""
                        FROM ""events""
                        WHERE ""id"" = {0}
                        FOR UPDATE";

            var eventEntity = await _context.Events
                .FromSqlRaw(sql, id)
                .FirstOrDefaultAsync(ct);

            return eventEntity;
        }

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }

    }
}

using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventMgtApi.Infrastructure.Repositories
{
    /// <summary>
    /// Потокобезопасная реализация <see cref="IEventRepository"/>, 
    /// хранящая события в памяти.
    /// Подходит для тестирования и прототипирования.
    /// </summary>
    public class InMemoryEventRepository : IEventRepository
    {
        private readonly ConcurrentDictionary<Guid, Event> _events = new();

        /// <inheritdoc />
        public Task<List<Event>> GetAllAsync()
        {
            var events = _events.Values.ToList();
            return Task.FromResult(events);
        }

        /// <inheritdoc />
        public Task<Event?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_events.TryGetValue(id, out var @event) ? @event : null);
        }

        /// <inheritdoc />
        public Task AddAsync(Event @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            _events[@event.Id] = @event;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> UpdateAsync(Event @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            return _events.ContainsKey(@event.Id) && _events.TryUpdate(@event.Id, @event, _events[@event.Id])
                ? Task.FromResult(true)
                : Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(Guid id)
        {
            return _events.TryRemove(id, out _)
                ? Task.FromResult(true)
                : Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<int> CountAsync()
        {
            return Task.FromResult(_events.Count);
        }
    }
}
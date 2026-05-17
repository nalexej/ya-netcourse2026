using EventMgtApi.Models;

namespace EventMgtApi.Repositories
{
    /// <summary>
    /// Потокобезопасная реализация <see cref="IEventRepository"/>, 
    /// хранящая события в памяти.
    /// Подходит для тестирования и прототипирования.
    /// </summary>
    public class InMemoryEventRepository : IEventRepository
    {
        private readonly List<Event> _events = new();
        private readonly object _lock = new();

        /// <summary>
        /// Возвращает копию всех событий.
        /// </summary>
        /// <returns>Список всех событий.</returns>
        public List<Event> GetAll()
        {
            lock (_lock)
            {
                return [.. _events];
            }
        }

        /// <summary>
        /// Ищет событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        /// <returns>Найденное событие или <see langword="null"/>, если не найдено.</returns>
        public Event? GetById(Guid id)
        {
            lock (_lock)
            {
                return _events.FirstOrDefault(e => e.Id == id);
            }
        }

        /// <summary>
        /// Добавляет новое событие в хранилище.
        /// </summary>
        /// <param name="event">Событие для добавления. Должно быть ненулевым.</param>
        public void Add(Event @event)
        {
            lock (_lock)
            {
                _events.Add(@event);
            }
        }

        /// <summary>
        /// Обновляет существующее событие по Id.
        /// </summary>
        /// <param name="event">Событие с обновлёнными данными. Должно иметь существующий Id.</param>
        /// <returns><see langword="true"/>, если событие найдено и обновлено; иначе <see langword="false"/>.</returns>
        public bool Update(Event @event)
        {
            lock (_lock)
            {
                var index = _events.FindIndex(e => e.Id == @event.Id);
                if (index == -1) return false;

                _events[index] = @event;
                return true;
            }
        }

        /// <summary>
        /// Удаляет событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого события.</param>
        /// <returns><see langword="true"/>, если событие удалено; иначе <see langword="false"/>.</returns>
        public bool Delete(Guid id)
        {
            lock (_lock)
            {
                var index = _events.FindIndex(e => e.Id == id);
                if (index == -1) return false;

                _events.RemoveAt(index);
                return true;
            }
        }

        /// <summary>
        /// Возвращает общее количество событий в хранилище.
        /// </summary>
        /// <returns>Число событий.</returns>
        public int Count()
        {
            lock (_lock)
            {
                return _events.Count;
            }
        }
    }
}
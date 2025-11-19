using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.DatasetEvents
{
    /// <summary>
    /// Raised when a dataset is updated
    /// </summary>
    public class DatasetUpdatedEvent : IDomainEvent
    {
        public Dataset Dataset { get; }
        public Dictionary<string, object> Changes { get; }
        public DateTime OccurredOn { get; }

        public DatasetUpdatedEvent(Dataset dataset, Dictionary<string, object> changes)
        {
            Dataset = dataset;
            Changes = changes;
            OccurredOn = DateTime.UtcNow;
        }
    }
}

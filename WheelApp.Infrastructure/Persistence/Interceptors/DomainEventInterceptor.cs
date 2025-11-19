using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WheelApp.Domain.Common;
using WheelApp.Domain.Events;

namespace WheelApp.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor for collecting and publishing domain events after successful save
    /// </summary>
    public class DomainEventInterceptor : SaveChangesInterceptor
    {
        private readonly IMediator _mediator;

        public DomainEventInterceptor(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                await PublishDomainEventsAsync(eventData.Context, cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
        {
            // Collect all domain events from tracked entities
            var entitiesWithEvents = context.ChangeTracker
                .Entries<Entity>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToList();

            // Collect all events
            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Clear events from entities before publishing to prevent re-publishing
            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }

            // Publish each event via MediatR
            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }
    }
}

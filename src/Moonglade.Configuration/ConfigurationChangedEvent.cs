using LiteBus.Events.Abstractions;

namespace Moonglade.Configuration;

public record ConfigurationChangedEvent(IReadOnlyCollection<string> Names) : IEvent;

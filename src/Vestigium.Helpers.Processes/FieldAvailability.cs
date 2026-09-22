namespace Vestigium.Helpers.Processes;

/// <summary>One field that is not <see cref="Availability.Available"/>.</summary>
public readonly record struct FieldAvailability(ProcessField Field, Availability State, string? Reason);

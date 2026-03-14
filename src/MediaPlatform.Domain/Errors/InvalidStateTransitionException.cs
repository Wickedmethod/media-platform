namespace MediaPlatform.Domain.Errors;

public sealed class InvalidStateTransitionException(string message) : Exception(message);

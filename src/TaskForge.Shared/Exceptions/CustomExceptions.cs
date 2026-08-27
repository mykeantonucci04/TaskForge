namespace TaskForge.Shared.Exceptions;

public abstract class BaseException : Exception
{
    protected BaseException(string message) : base(message) { }
}

public class DomainException : BaseException
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : BaseException
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.") { }
}

public class ValidationException : BaseException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message) { }
}

public class ForbiddenException : BaseException
{
    public ForbiddenException(string message = "Forbidden access.") : base(message) { }
}

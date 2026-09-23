namespace Domain.Exceptions;

public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationAppException(string propertyName, string errorMessage) : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }

    public ValidationAppException(IDictionary<string, string[]> errors) 
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

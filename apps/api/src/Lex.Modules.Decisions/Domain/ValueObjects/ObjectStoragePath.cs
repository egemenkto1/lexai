using Lex.SharedKernel;

namespace Lex.Modules.Decisions.Domain.ValueObjects;

public sealed class ObjectStoragePath : ValueObject
{
    public string Key { get; }

    public ObjectStoragePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage path cannot be empty");
        Key = key;
    }

    public override IEnumerable<object> GetEqualityComponents() { yield return Key; }
}

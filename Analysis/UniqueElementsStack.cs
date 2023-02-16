using System.Diagnostics.CodeAnalysis;

namespace Analysis;

public sealed class UniqueElementsStack<T> where T : notnull
{
    private readonly Stack<T> _stack = new();
    private readonly Dictionary<T, int> _indices = new();

    public int Count => _stack.Count;
    
    public bool TryPush(T element, out int existingIndex)
    {
        if (_indices.TryGetValue(element, out existingIndex))
        {
            return false;
        }
        
        _indices.Add(element, _stack.Count);
        _stack.Push(element);
        return true;
    }

    public bool TryPeek([MaybeNullWhen(false)] out T element)
    {
        return _stack.TryPeek(out element);
    }

    public T Pop()
    {
        return _stack.Pop();
    }

    public void Push(T element)
    {
        if (!TryPush(element, out _))
        {
            throw new InvalidOperationException("Stack already contains specified element");
        }
    }
}
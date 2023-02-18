using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Analysis;

public sealed class UniqueElementsStack<T> where T : notnull
{
    private readonly Stack<T> _stack = new();
    private readonly HashSet<T> _existingElements = new();

    public bool TryPush(T element)
    {
        if (!_existingElements.Add(element))
        {
            return false;
        }
        
        _stack.Push(element);
        return true;
    }

    public bool TryPeek([MaybeNullWhen(false)] out T element)
    {
        return _stack.TryPeek(out element);
    }

    public T Pop()
    {
        var element = _stack.Pop();
        var removed = _existingElements.Remove(element);
        Debug.Assert(removed, "Can't delete existing element");
        return element;
    }

    public void Push(T element)
    {
        if (!TryPush(element))
        {
            throw new InvalidOperationException("Stack already contains specified element");
        }
    }
}
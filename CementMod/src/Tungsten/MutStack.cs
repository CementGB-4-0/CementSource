using System;

namespace Tungsten;
// mutable stack
public class MutStack<T>
{
    public int top { get; private set; }
    public int indexBase = 0;
    private T[] _array;

    public MutStack(int capacity = 4)
    {
        _array = new T[4];
    }

    public T[] PeekTop(int amount)
    {
        return amount == 0 ? (new T[0]) : _array.AsSpan(top - amount, amount).ToArray();
    }


    public T Peek()
    {
        return _array[top - 1];
    }

    public void Clear()
    {
        _array = new T[4];
        top = 0;
        indexBase = 0;
    }

    // mutating part
    public T this[int i]
    {
        get => _array[indexBase + i];

        set => _array[indexBase + i] = value;
    }


    public T Pop()
    {
        /*
        if (top - 1 < _array.Length / 2)
        {
            T[] newArray = new T[_array.Length / 2];
            _array.CopyTo(newArray, 0);
            _array = newArray;
        }*/
        return _array[--top];
    }

    public void Pop(int amt)
    {
        /*
        if (top - amt < _array.Length / 2)
        {
            T[] newArray = new T[_array.Length / 2];
            _array.CopyTo(newArray, 0);
            _array = newArray;
        }*/
        top -= amt;
    }

    public void Push(T val)
    {
        if (top + 1 >= _array.Length)
        {
            var newArray = new T[_array.Length * 2];
            _array.CopyTo(newArray, 0);
            _array = newArray;
        }
        _array[top++] = val;
    }
}

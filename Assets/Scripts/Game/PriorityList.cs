using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using static Utils;

//namespace Game;

/// <summary>
///     A sorted list. This name was chosen since SortedLit was already taken (by something that
/// doesn't really look like a sorted list). This implementation is not efficient, but that won't be any source of
/// problems in this project
/// </summary>
/// <typeparam name="TElement">The type of elements contained in the queue</typeparam>
public class PriorityList<TElement>
{
    private List<TElement> _elements;
    private Func<TElement, float> _sorter;

    public PriorityList(ICollection<TElement> collection, Func<TElement, float> sorter)
    {
        _elements = new List<TElement>(collection);
        _sorter = sorter;
        SortQueue();
    }

    public void Add(TElement element)
    {
        _elements.Add(element);
        SortQueue();
    }

    public void Add(ICollection<TElement> collection)
    {
        _elements.AddRange(collection);
        SortQueue();
    }

    public TElement Peek() => _elements[0];

    public void Remove()
    {
        if (_elements.Count == 0) return;
        _elements[0] = default(TElement);
        SortQueue();
    }

    private void SortQueue()
    {
        _elements = _elements.OrderBy(e => _sorter(e)).ToList();
        _elements.RemoveAll(IsNull);
    }

    public override string ToString()
    {
        return $"Queue [elements: {ListToString(_elements)}]";
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Utils;

//namespace Game;

/// <summary>
///     A sorted list. This name was chosen since SortedLit was already taken (by something that
/// doesn't really look like a sorted list). This implementation is not efficient, but that won't be any source of
/// problem in this project
/// </summary>
/// <typeparam name="TElement">The type of elements contained in the queue</typeparam>
public class PriorityList<TElement> : IEnumerable<TElement>
{
    private List<TElement> _elements;
    private Func<TElement, float> _sorter;

    public PriorityList(Func<TElement, float> sorter)
    {
        _elements = new List<TElement>();
        _sorter = sorter;
    }

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

    public TElement Peek()
    {
        if (IsEmpty()) throw new InvalidOperationException("The priority queue is empty!");
        return _elements[0];
    } 

    public void Dequeue()
    {
        if (_elements.Count == 0) return;
        //_elements[0] = default(TElement);
        _elements.Remove(_elements[0]);
        SortQueue();
    }

    public bool IsEmpty() => _elements.Count == 0;

    private void SortQueue()
    {
        _elements.RemoveAll(IsNull);
        _elements = _elements.OrderBy(e => _sorter(e)).ToList();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<TElement> GetEnumerator()
    {
        return _elements.GetEnumerator();
    }

    public override string ToString()
    {
        return $"PriorityList [elements: {ListToString(_elements)}]";
    }

    public string ToFullString()
    {
        if (_elements.Count == 0) return "PriorityList []";
        if (_elements.Count == 1)
        {
            return "PriorityList [element: {" + _elements[0] + ", " + _sorter(_elements[0]) + "}]";
        }

        string s = "PriorityList [";
        foreach (TElement element in _elements)
        {
            s += "{" + element + ", " + _sorter(element) +"}" + ", ";
        }
        
        s = s.Remove(s.Length - 2, 2);
        return "PriorityList[elements: " + s + "]";
    }

}
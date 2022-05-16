using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//namespace Game;

public static class Utils
{
    
    private const float EPSILON = 1e-4f;   // Tolerance regarding floating point values equality

    public static bool AreSame(float a, float b)
    {
        return Math.Abs(a - b) < EPSILON;
    }

    public static bool AreSame(Vector2 position1, Vector2 position2)
    {
        return AreSame(Vector2.Distance(position1, position2), 0);
    }

    public static string ListToString<T>(ICollection<T> collection) => ListToString(collection, t => t.ToString());
    public static string ListToString<T>(ICollection<T> collection, Func<T, string> formatting)
    {
        if (collection.Count == 0) return "{}";
        if (collection.Count == 1)
        {
            return "{" + formatting(collection.First()) + "}";
        }

        string s = "{";
        foreach (T element in collection)
        {
            s += formatting(element) + ", ";
        }
        
        return s.Remove(s.Length - 2, 2) + "}";
    }

    public static TElement MinElement<TElement>(TElement element1, TElement element2, Func<TElement, float> sorter)
    {
        return sorter(element1) < sorter(element2) ? element1 : element2;
    }

    public static TElement MinElement<TElement>(ICollection<TElement> collection, Func<TElement, float> sorter)
    {
        if (collection.Count == 0) throw new ArgumentException("Collection is null!");
        TElement minElement = collection.ToList()[0];
        float minScore = float.MaxValue;
        foreach (TElement element in collection)
        {
            float newScore = sorter(element);
            if (newScore < minScore)
            {
                minScore = newScore;
                minElement = element;
            }
        }

        return minElement;
    }

    public static bool IsNull<TElement>(TElement element)
    {
        return EqualityComparer<TElement>.Default.Equals(element, default(TElement));
    }

    public static Vector2 ToVector2(Vector3 vector) => new Vector2(vector.x, vector.z);
    public static Vector3 ToVector3(Vector2 vector, float height) => new Vector3(vector.x, height, vector.y);
}
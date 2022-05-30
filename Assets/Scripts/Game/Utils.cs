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

    public static TElement MaxElement<TElement>(ICollection<TElement> collection, Func<TElement, float> sorter)
    {
        if (collection.Count == 0) throw new ArgumentException("Collection is null!");
        TElement maxElement = collection.ToList()[0];
        float maxScore = float.MinValue;
        foreach (TElement element in collection)
        {
            float newScore = sorter(element);
            if (newScore > maxScore)
            {
                maxScore = newScore;
                maxElement = element;
            }
        }

        return maxElement;
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

    /// Gets a value from a dictionary. Just syntaxic sugar
    public static TValue GetFrom<TKey, TValue>(Dictionary<TKey, TValue> dictionnary, TKey key)
    {
        //if (!dictionnary.Contains<>(key)) throw new ArgumentException("Dictionnary does not contain key");
        TValue value;
        dictionnary.TryGetValue(key, out value);
        return value;
    }

    /// Returns the angle between a given vector and the horizontal right. The output angle is counter-clockwise and
    /// between in [0,360[. For e.g, the vector (1,0) will have an angle of 0, (-1,0) will be 180, and (0,-1) will be 270
    public static float Angle(Vector2 vector)
    {
        float angle = Vector2.SignedAngle(Vector2.right, vector);
        return angle >= 0 ? angle : angle + 360;
    }

    public static float AngleBetween(Vector2 from, Vector2 to)
    {
        return Vector2.SignedAngle(-from, to) + 180;
    }

    public static float ToRadians(float degrees) => (float)(degrees * Math.PI / 180);
    public static float ToDegrees(float radians) => (float)(radians * 180 / Math.PI);
    
    public static Vector2 ToVector2(Vector3 vector) => new Vector2(vector.x, vector.z);
    public static Vector3 ToVector3(Vector2 vector, float height) => new Vector3(vector.x, height, vector.y);
    
}
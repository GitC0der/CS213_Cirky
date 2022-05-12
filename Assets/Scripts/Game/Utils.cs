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

    public static string ListToString<T>(ICollection<T> collection)
    {
        if (collection.Count == 1)
        {
            return "{" + collection.First() + "}";
        }

        string s = "{";
        foreach (T element in collection)
        {
            s += element + ", ";
        }

        s.Remove(s.Length - 2, 2);
        return s + "}";
    }

    public static Vector2 ToVector2(Vector3 vector) => new Vector2(vector.x, vector.y);
    public static Vector3 ToVector3(Vector2 vector, float height) => new Vector3(vector.x, height, vector.y);
}
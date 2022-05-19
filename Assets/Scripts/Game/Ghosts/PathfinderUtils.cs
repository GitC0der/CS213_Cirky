using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Pathfinder;
using static CircularMap;
using static Utils;

//namespace Game.Ghosts;

public class PathfinderUtils
{
    public static string DrawGraph(Pathfinder pathfinder)
    {
        List<Node> nodes = pathfinder.Nodes().OrderBy(n => Vector2.Distance(n.Position(), pathfinder.Map().Center())).ToList();
        
        float radius = Vector2.Distance(nodes[0].Position(), pathfinder.Map().Center());
        string str = "";
        int ringID = 0;
        foreach (Node node in nodes)
        {
            float newRadius = Vector2.Distance(node.Position(), pathfinder.Map().Center());
            if (Mathf.Abs(newRadius - radius) < float.Epsilon)
            {
                radius = newRadius;
                ++ringID;
                if (str.Length >= 2) str = str.Remove(str.Length - 2, 2);
                str += $"\nRing with radius {radius} : ";
            }

            str = str + node.ToSimpleString() + ", ";
        }
        str = str.Remove(str.Length - 2, 2);
        return $"Graph [{str}]";

    }
}
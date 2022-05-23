using System;
using static Utils;
using static CircularMap;
using static Pathfinder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/**
 *  The Unity Test Framework was way too hard to set up so this ugly solution is used.
 */
public class UnitTests : MonoBehaviour
{
    CircularMap map = new CircularMap(new Vector2(3, 3), 9.1f);

    private void LaunchTests()
    {
        //FindExistingNode_Works();
        //MapGeneration_Tests();
        //Pathfinder_OtherNode_Works();
        //Pathfinder_Connect_Works();
        //MapRing_Direction_Works();
    }

    private void NeighborsAdd_Test()
    {
        Node node1 = new Node(new Vector2(3, 7));
        Debug.Log($"Node1 is {node1}");
        Node node2 = new Node(new Vector2(-2, 4));
        node1.Neighbors().Add(node2);
        Debug.Log($"Node1 is {node1}");
        Debug.Log($"Node2 is {node2}");
    }

    private void Pathfinder_OtherNode_Works()
    {
        MapRing ring = new MapRing(3, new Vector2(-1,6));
        Edge edge = new Edge(new Node(new Vector2(-1,9)), new Node(new Vector2(2,6)), 3, ring);
        Debug.Log($"Node1 = {edge.Node1()}");
        Debug.Log($"Node2 = {edge.Node2()}");
        Debug.Log($"OtherNode of node1 is {edge.OtherNode(edge.Node1())}");
        Debug.Log($"OtherNode of node2 is {edge.OtherNode(edge.Node2())}");
    }
    private void Pathfinder_Connect_Works()
    {
        Node node1 = new Node(new Vector2(4, 7));
        Node node2 = new Node(new Vector2(-2,3));
        Debug.Log($"Node1 not connected is {node1}");
        Debug.Log($"Node2 not connected is {node2}");
        MapRing ring = new MapRing(5, new Vector2(6,6));
        node1.Connect(node2, ring.DistanceBetween(node1.Position(), node2.Position()), ring);
        Debug.Log($"Node1 connected is {node1}");
        Debug.Log($"Node2 connected is {node2}");
        node2.Connect(node1, 12, ring);
        Debug.Log($"Node1 after failed connection is {node1}");
        Debug.Log($"Node2 after failed connection is {node2}");
    }

    private void MapGeneration_Tests()
    {
        CircularMap map = new CircularMap(new Vector2(3, 5), 2);
        Debug.Log($"Empty map is {map}");
        map.AddPassage(0, new Vector2(0,1));
        Debug.Log($"Passage should go from (3,7) to (3,9), actually is {map}");
        map.AddPassage(new Vector2(6,5));
        Debug.Log($"Passage should go from (5,5) to (7,5), actually is {map}");
    }

    private void AngleTests()
    {
        Vector2 v1 = new Vector2(0, 1);
        Vector2 v2 = new Vector2(1,1);
        Vector2 v3 = new Vector2(-1, 0);
        Assert.AreEqual(45, Vector2.Angle(v1,v2));
        Assert.AreEqual(45, Vector2.Angle(v2, v1));
        Assert.AreEqual(135, Vector2.Angle(v2,v3));
        Assert.AreEqual(135, Vector2.Angle(v3,v2));
        
        Debug.Log($"Angle between {v1} and {v2} is {Vector2.SignedAngle(v1,v2)}");
        Debug.Log($"Angle between {v2} and {v1} is {Vector2.SignedAngle(v2,v1)}");
        Debug.Log($"Angle between {v2} and {v3} is {Vector2.SignedAngle(v2,v3)}");
        Debug.Log($"Angle between {v3} and {v2} is {Vector2.SignedAngle(v3,v2)}");
        Debug.Log($"Angle between {v1} and {v3} is {Vector2.SignedAngle(v1,v3)}");
        Debug.Log($"Angle between {v3} and {v1} is {Vector2.SignedAngle(v3,v1)}");
    }

    private void List_AddRange_Works()
    {
        List<int> initial = new List<int> { 3, 8 };
        List<int> added = new List<int> { -5, 17 };
        initial.AddRange(added);
        Debug.Log($"Expected = {ListToString(new List<int> {3,8,-5,17})} | Actual = {ListToString(initial)}");
    }

    private void MapRing_DistanceBetween_Works()
    {
        float radius = 5;
        Vector2 center = new Vector2(2, 2);
        MapRing ring = new MapRing(radius, center);
        float distance1A = ring.DistanceBetween(new Vector2(2, 7), new Vector2(7, 2), false);
        float distance2A = ring.DistanceBetween(new Vector2(2, 7), new Vector2(7, 2), true);
        float distance1B = ring.DistanceBetween(new Vector2(7, 2), new Vector2(2, 7), false);
        float distance2B = ring.DistanceBetween(new Vector2(7, 2), new Vector2(2, 7), true);
        Assert.AreApproximatelyEqual((float)(Math.PI*radius/2.0), distance1A);
        Assert.AreApproximatelyEqual((float)(3*Math.PI*radius/2.0), distance2A);
        Assert.AreApproximatelyEqual((float)(Math.PI*radius/2.0), distance1B);
        Assert.AreApproximatelyEqual((float)(3*Math.PI*radius/2.0), distance2B);
        
        Debug.Log($"{ring.DistanceBetween(new Vector2(5,5), new Vector2(1,-5), false)}");
        Debug.Log($"{ring.DistanceBetween(new Vector2(5,5), new Vector2(1,-5), true)}");
        Debug.Log($"{ring.DistanceBetween(new Vector2(1,-5), new Vector2(5,5), false)}");
        Debug.Log($"{ring.DistanceBetween(new Vector2(1,-5), new Vector2(5,5), true)}");
    }
    private void PriorityQueue_Works()
    {
        PriorityList<int> list = new PriorityList<int>(new List<int>{2, 7, -3, 9, 11, -15 }, 
            e => Math.Abs(e));
        Debug.Log(list);
        Assert.AreEqual(2, list.Peek());
        list.Dequeue();
        Debug.Log(list);
        Assert.AreEqual(-3, list.Peek());
        list.Dequeue();
        Debug.Log(list);
        Assert.AreEqual(7, list.Peek());
    }
    private void Utils_MinElement_Works()
    {
        List<int> list = new List<int> { 2, -5, -7, 3, 6 };
        Func<int, float> function = i => (float)Math.Pow(i / 4.0, 2);
        Assert.AreEqual(2, MinElement(list, function));
        Assert.AreEqual(2, MinElement(2, -5, function));

        MapRing expected = new MapRing(3, new Vector2(1, 2));
        MapRing false1 = new MapRing(2, new Vector2(13, 15));
        List<MapRing> ringList = new List<MapRing> { expected, 
            false1, new MapRing(5, new Vector2(21,7)) };
        Func<MapRing, float> ringFunction = r => Vector2.Distance(r.Center(), new Vector2(3, 6));
        Assert.AreEqual(expected, MinElement(ringList, ringFunction));
        Assert.AreEqual(expected, MinElement(expected, false1, ringFunction));
    }

    private void CircularMap_ClosestRings_Works()
    {
        MapRing expected1A = new MapRing(map, 0);
        MapRing expected1B = new MapRing(map, 1);
        MapRing expected2A = new MapRing(map, 2);
        MapRing expected2B = new MapRing(map, 3);
        IList<MapRing> actual1 = map.ClosestRings(new Vector2(2.3f, 1));
        IList<MapRing> actual2 = map.ClosestRings(map.Center() + new Vector2(1,-1).normalized * 6.5f);

        Debug.Log($"Expected1A = {expected1A} | actual1 = {ListToString(actual1)}");
        Debug.Log($"Expected1B = {expected1B} | actual1 = {ListToString(actual1)}");
        Debug.Log($"Expected2A = {expected2A} | actual2 = {ListToString(actual2)}");
        Debug.Log($"Expected2B = {expected2B} | actual2 = {ListToString(actual2)}");
    }

    private void MapRing_Direction_Works()
    {
        MapRing ring = new MapRing(5, new Vector2(1, 1));
        Vector2 direction1 = ring.Direction(new Vector2(1,6), true);
        Vector2 direction2 = ring.Direction(new Vector2(1,6), false);
        Vector2 direction3 = ring.Direction(new Vector2(3.5f,3.5f), true);
        Vector2 direction4 = ring.Direction(new Vector2(6,1), true);

        Vector2 expected1 = new Vector2(1, 0).normalized;
        Vector2 expected2 = new Vector2(-1, 0).normalized;
        Vector2 expected3 = new Vector2(1, -1).normalized;
        Vector2 expected4 = new Vector2(0, -1).normalized;
        Assert.AreEqual(direction1, expected1, $"Direction1 is {direction1} : should be parallel to {expected1}");
        Assert.AreEqual(direction2, expected2, $"Direction1 is {direction2} : should be parallel to {expected2}");
        Assert.AreEqual(direction3, expected3, $"Direction1 is {direction3} : should be parallel to {expected3}");
        Assert.AreEqual(direction4, expected4, $"Direction1 is {direction4} : should be parallel to {expected4}");
    }

    private void FindExistingNode_Works()
    {
        var direction1 = new Vector2(1, 4);
        var direction2 = new Vector2(-1, -1);
        map.AddPassage(0, direction1);
        map.AddPassage(0, direction2);
        map.AddPassage(1, direction1);
        map.AddPassage(2, direction2);
        map.AddPassage(2, direction1);
        Debug.Log(map);
        Pathfinder pf = new Pathfinder(map);
        Debug.Log(pf);
        
    }

    private void MapRingTests()
    {
        CircularMap.MapRing ring = new CircularMap.MapRing(5, new Vector2(3,9));
        Debug.Log(ring);
    }

    private void PassagesOnRingTest()
    {
        Vector2 center = new Vector2(0,0);
        CircularMap.MapRing ring1 = new CircularMap.MapRing(4, center);
        CircularMap.MapRing ring2 = new CircularMap.MapRing(9, center);
        CircularMap.MapRing falseRing = new CircularMap.MapRing(9, new Vector2(4,8));
        CircularMap.Passageway passage1 = new CircularMap.Passageway(ring1, ring2, new Vector2(1, 2));
        CircularMap.Passageway passage2 = new CircularMap.Passageway(ring1, ring2, new Vector2(1, -0.5f));
        CircularMap.Passageway falsePassage = new CircularMap.Passageway(ring1, falseRing, new Vector2(2,3));
        CircularMap map = new CircularMap(center, 12, new List<CircularMap.Passageway> {passage1, passage2, falsePassage});
        Debug.Log(ListToString(map.PassagesOnRing(ring1)));
        Debug.Log(ListToString(map.PassagesOnRing(falseRing)));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("------------ STARTING DEBUG ------------");
        LaunchTests();
        Debug.Log("------------ DEBUG COMPLETE ------------");

    }
}

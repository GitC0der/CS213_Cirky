using static Utils;
using System;
using System.Collections.Generic;
using UnityEngine.Diagnostics;
using Vector2 = UnityEngine.Vector2;
using MapRing = CircularMap.MapRing;
using Passageway = CircularMap.Passageway;

//namespace Game.Ghosts;

public class Pathfinder
{
    public const float BASE_COST = 1;

    private readonly CircularMap _map;
    private ISet<Node> _nodes;

    public Pathfinder(CircularMap map)
    {
        _map = map;
        foreach (Passageway passage in _map.Passages())
        {
            Node node1 = new Node(passage.SmallPoint());
            Node node2 = new Node(passage.LargePoint());
            node1.Connect(node2, passage.Length());
            //_nodes.Add();
        }
        
        foreach (MapRing ring in _map.Rings())
        {
            
        }
    }

    public Vector2 ComputeOrientation(Vector2 current, Vector2 next)
    {
        return new Vector2();
    }
    
    public class Edge
    {
        private Node _node1;
        private Node _node2;
        private float _length;
        private float _cost;

        public Edge(Node node1, Node node2, float length, float cost = BASE_COST)
        {
            if (node1 == null || node2 == null) throw new ArgumentException("Node1 and node2 can't be null!");
            if (length <= 0) throw new ArgumentException("Length must be greater than 0!");
            _node1 = node1;
            _node2 = node2;
            _length = length;
        }

        public ISet<Node> Nodes() => new HashSet<Node> { _node1, _node2 };

        public float Length() => _length;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Edge)obj;
            return _node1.Equals(other._node1) && _node2.Equals(other._node2);
        }
        
        public override string ToString()
        {
            return $"Edge[node1 = {_node1}, node2 = {_node2}, length = {_length,3:F2}, cost = {_cost,3:F2}]";
        }
    }
    public class Node
    {
        private Vector2 _position;
        private ISet<Edge> _edges;
        private ISet<Node> _neighbors;

        public Node(Vector2 position) : this(position, new HashSet<Edge>()) {}
        public Node(Vector2 position, ICollection<Edge> edges)
        {
            _position = position;
            _edges = new HashSet<Edge>(edges);
        }

        public bool IsConnectedTo(Node otherNode)
        {
            return false;
        }

        public ISet<Node> Neighbors()
        {
            foreach (Edge edge in _edges)
            {
                
            }
            return new HashSet<Node>();
        }

        public void Connect(Node otherNode, float length, float cost = BASE_COST)
        {
            Connect(new Edge(this, otherNode, length, cost));
        }

        public void Connect(Edge edge)
        {
            _edges.Add(edge);
        }

        public Vector2 Position() => _position;

        public ISet<Edge> Edges() => _edges;
        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Node)obj;
            return AreSame(_position, other._position);
        }

        public string ToSimpleString()
        {
            return $"Node[psotion = {_position}]";
        }

        public override string ToString()
        {
            return $"Node[position = {_position}, neighbors = {ListToString(_neighbors)}]";
        }
    }
}
using static Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Diagnostics;
using Vector2 = UnityEngine.Vector2;
using MapRing = CircularMap.MapRing;
using Passageway = CircularMap.Passageway;

//namespace Game.Ghosts;

public class Pathfinder
{
    public const float BASE_COST = 1;
    public const float OCCUPIED_COST = 10e3f;

    private readonly CircularMap _map;
    private ISet<Node> _nodes = new HashSet<Node>();
    
    public Pathfinder(CircularMap map)
    {
        _map = map;
        foreach (Passageway passage in _map.Passages())
        {
            Node node1 = new Node(passage.SmallPoint());
            Node node2 = new Node(passage.LargePoint());
            node1.Connect(node2, passage.Length());
            _nodes.Add(node1);
            _nodes.Add(node2);
        }

        foreach (MapRing ring in map.Rings())
        {
            List<Vector2> positions = _map.PassagesOnRing(ring).ToList().OrderBy(pos => Vector2.Angle(Vector2.right, pos)).ToList();
            //List<Vector2> positions = _map.PassagesOnRing(ring).ToList();
            //positions.Sort((v1,v2) => Vector2.Angle(Vector2.right, v1).CompareTo(Vector2.Angle(Vector2.right, v2)));
            Node current = null; Node next = null;
            if (positions.Count > 1)
            {
                Node first = FindExistingNode(new Node(positions[0]));
                for (int i = 0; i < positions.Count - 1; ++i)
                {
                    current = FindExistingNode(new Node(positions[i]));
                    next = FindExistingNode(new Node(positions[i + 1]));
                    current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()));
                }
                next.Connect(first, ring.DistanceBetween(next.Position(), first.Position()));
            }
        }
    }

    /** Returns the similar node already present when possible, otherwise returns the given node */
    public Node FindExistingNode(Node node)
    {
        foreach (Node possibleNode in _nodes)
        {
            if (node.Equals(possibleNode)) return possibleNode;
        }

        return node;
    }

    public Vector2 ComputeOrientation(Vector2 current, Vector2 next)
    {
        return new Vector2();
    }
    
    public override string ToString()
    {
        return $"Pathfinder[nodes = {ListToString(_nodes)}]";
    }
    
    public class Edge
    {
        private Node _node1;
        private Node _node2;
        private float _length;
        private float _cost;

        public Edge(Node node1, Node node2, float length, float cost = BASE_COST)
        {
            if (node1.Equals(node2)) throw new ArgumentException("Nodes must be different from one another!");
            if (length <= 0) throw new ArgumentException("Length must be greater than 0!");
            _node1 = node1;
            _node2 = node2;
            _length = length;
        }

        public ISet<Node> Nodes() => new HashSet<Node> { _node1, _node2 };
        
        public Node OtherNode(Node node) => _node1.Equals(node)? _node1: _node2;

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
            _neighbors = new HashSet<Node>();
            foreach (Edge edge in _edges)
            {
                foreach (Node node in edge.Nodes())
                {
                    _neighbors.Add(node);
                }
            }
        }

        public bool IsNextTo(Node otherNode)
        {
            foreach (Edge edge in _edges)
            {
                if (edge.OtherNode(this).Equals(otherNode)) return true;
            }
            return false;
        }

        public bool IsConnectedTo(Node otherNode)
        {
            //TODO: Complete this!
            return false;
        }

        public ISet<Node> Neighbors() => new HashSet<Node>(_neighbors);

        public void Connect(Node otherNode, float length, float cost = BASE_COST)
        {
            Connect(new Edge(this, otherNode, length, cost));
        }

        public void Connect(Edge edge)
        {
            _edges.Add(edge);
            _neighbors.Add(edge.OtherNode(this));
        }

        public Vector2 Position() => _position;
        
        public ISet<Edge> Edges() => new HashSet<Edge>(_edges);
        
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
using static Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Diagnostics;
using Vector2 = UnityEngine.Vector2;
using MapRing = CircularMap.MapRing;
using Passageway = CircularMap.Passageway;
using IPathway = CircularMap.IPathway;

//namespace Game.Ghosts;

public class Pathfinder
{
    private const float BASE_COST = 1;
    private const float OCCUPIED_COST = 10e3f;  //TODO: Use this
    private const float TOLERANCE = 0.2f;

    private readonly CircularMap _map;
    private ISet<Node> _nodes = new HashSet<Node>();

    private Vector2 _direction;
    
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
                    if (!current.IsConnectedTo(next) && !current.Equals(next))
                    {
                        current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()));
                    }
                }
                next.Connect(first, ring.DistanceBetween(next.Position(), first.Position()));
            }
        }
        
    }

    public void MergeNodes(Node node1, Node node2)
    {
        //TODO: Implement this if necessary
    }

    public void AddNode(IPathway path, Edge edge, Vector2 newPosition)
    {
        //if (path.DistanceTo(edge.Node1().Position()) < path.DistanceTo(edge.Node2().Position()))
        Node keptNode;
        if (path.DistanceBetween(edge.Node1().Position(), newPosition) 
            < path.DistanceBetween(edge.Node2().Position(), newPosition))
        {
            keptNode = edge.Node1();
        }
        else
        {
            keptNode = edge.Node2();
        } 
        keptNode.SplitConnection(edge, new Node(newPosition), path.DistanceBetween(keptNode.Position(), newPosition));
    }

    /* Note : can only remove cellulo nodes, which are nodes that do not serve as a junction (i.e only have 2 edges) **/
    public void RemoveNode(IPathway path, Node node)
    {
        if (node.Edges().Count != 2)
            throw new ArgumentException(
                $"Can only remove edges that do not serve as a junction (i.e it mst have exactly 2 edges). " +
                $"It instead had {node.Edges().Count} edges");
        Node node1 = node.Edges().ToList()[0].OtherNode(node);
        Node node2 = node.Edges().ToList()[1].OtherNode(node);
        node1.Connect(node2, path.DistanceBetween(node1.Position(), node2.Position()));
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

    public List<Node> FindClosestNodesOn(IPathway path, Vector2 position)
    {
        List<Node> newNodes = new List<Node>(_nodes);
        newNodes.RemoveAll(n => path.DistanceBetween(n.Position(), position) < TOLERANCE);
        newNodes = newNodes.OrderBy(n => path.DistanceBetween(n.Position(), position)).ToList();
        return new List<Node> { newNodes[0], newNodes[1] };
    }
    
    public Vector2 ComputeOrientation(Vector2 current, Vector2 next)
    {
        List<Node> frontier = new List<Node>();
        Node startNode = FindExistingNode(new Node(current));
        Node endNode = FindExistingNode(new Node(next));
        
        frontier.AddRange(startNode.Neighbors());
        Node currentNode = new Node(current);
        IPathway path = _map.FindClosestPathway(current);
        //currentNode.Connect(FindClosestNodesOn(, current));
        
        
        /*
        this.graph = graph;
        this.startNode = startNode;
        this.endNode = endNode;

        frontier.addAll(startNode.neighbors());
        currentNode = frontier.remove();
        neighbors.addAll(currentNode.neighbors());


        costSoFar.put(startNode, 0.0);
        //costSoFar.put(currentNode, graph.baseCostOf(currentNode).cost());
        addNodeToCostSoFar(currentNode, startNode);
        comeFrom.put(currentNode, startNode);
        for (Node node : frontier) {
            comeFrom.put(node, startNode);
            //costSoFar.put(node, graph.baseCostOf(node).cost());
            addNodeToCostSoFar(node, startNode);
        }
        */
        
        /*
        // When all nodes have been reached
        if (frontier.isEmpty() && costSoFar.size() > 2) {

            Node tempNode = endNode;
            finalPath.add(endNode);
            do {
                tempNode = comeFrom.get(tempNode);
                finalPath.add(tempNode);
            } while (comeFrom.get(comeFrom.get(tempNode)) != null);
            finalPath.add(comeFrom.get(tempNode));

            System.out.println("------ Final Path -----");
            System.out.println(finalPath);

            return;
        }

        // When moved to next frontier pathfinding.Node
        if (neighbors.isEmpty()) {
            currentNode = frontier.remove();
            neighbors = new LinkedBlockingQueue<>(currentNode.neighbors());
        }


        Node neighbor;
        do {
            neighbor = neighbors.remove();

        } while (!neighbors.isEmpty() && costSoFar.containsKey(neighbor));



        if (!costSoFar.containsKey(neighbor)) {
            //if (!costSoFar.containsKey(neighbor) || costSoFar.get(currentNode) + Math.max(graph.baseCostOf(neighbor).cost(), graph.baseCostOf(currentNode).cost()) < costSoFar.get(neighbor)) {
            frontier.add(neighbor);
            comeFrom.put(neighbor, currentNode);
            //costSoFar.put(neighbor, costSoFar.get(currentNode) + Math.max(graph.baseCostOf(neighbor).cost(), graph.baseCostOf(currentNode).cost()));
            addNodeToCostSoFar(neighbor, currentNode);
            if (neighbor.equals(endNode)) {
                System.out.println("--- Final path computed : follow the arrows starting from the end node ---");
            }
        }
        */
        return new Vector2(0, 0);
    }
    
    public override string ToString()
    {
        Func<Node, string> formatter = n => $"{n.ToSimpleString()}";
        return $"Pathfinder [nodes = {ListToString(_nodes, formatter)}]";
    }

    public class Edge
    {
        private Node _node1;
        private Node _node2;
        private float _length;
        private float _cost;
        private bool _isOccupied;

        public Edge(Node node1, Node node2, float length, float cost = BASE_COST, bool isOccupied = false)
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

        public bool IsOccupied() => _isOccupied;

        public Node Node1() => _node1;

        public Node Node2() => _node2;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Edge)obj;
            return _node1.Equals(other._node1) && _node2.Equals(other._node2);
        }
        
        public override string ToString()
        {
            return $"Edge [node1 = {_node1}, node2 = {_node2}, length = {_length,3:F2}, cost = {_cost,3:F2}]";
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
            foreach (Edge edge in _edges)
            {
                if (edge.OtherNode(this).Equals(otherNode)) return true;
            }
            return false;
        }

        public ISet<Node> Neighbors() => new HashSet<Node>(_neighbors);

        public void Connect(Node node, float length, float cost = BASE_COST)
        {
            Connect(new Edge(this, node, length, cost));
        }
        
        public void Connect(Edge edge)
        {
            Node otherNode = edge.OtherNode(this);
            _edges.Add(edge);
            _neighbors.Add(otherNode);
            if (!otherNode.IsConnectedTo(this)) otherNode.Connect(edge);
        }

        /* Reconnects this node to another one that is in the middle of a given edge **/
        public void SplitConnection(Edge edge, Node newNode, float length, float cost = BASE_COST)
        {
            _neighbors.Remove(edge.OtherNode(this));
            _edges.Remove(edge);
            _edges.Add(new Edge(this, newNode, length, cost));
            _neighbors.Add(newNode);
        }

        public void Disconnect(Node node)
        {
            
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
            return $"Node [position = {_position}]";
        }

        public override string ToString()
        {
            return $"Node [position = {_position}, neighbors = {ListToString(_neighbors, n => ToSimpleString())}]";
        }
    }
}

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
    private ISet<IPathway> _occupied = new HashSet<IPathway>();

    private Vector2 _direction;
    private List<Node> _finalNodes;
    private List<IPathway> _finalPath;
    
    public Pathfinder(CircularMap map)
    {
        _map = map;
        
        foreach (Passageway passage in _map.Passages())
        {
            Node node1 = new Node(passage.SmallPoint());
            Node node2 = new Node(passage.LargePoint());
            node1.Connect(node2, passage.Length(), passage);
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
                        current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()), ring);
                    }
                }
                next.Connect(first, ring.DistanceBetween(next.Position(), first.Position()), ring);
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
        keptNode.SplitConnection(edge, new Node(newPosition), path.DistanceBetween(keptNode.Position(), newPosition), path);
    }

    public void AddNode(Vector2 position)
    {
        IPathway pathway = _map.FindClosestPathway(position);
        AddNode(pathway, FindClosestNodesOn(pathway, position)[0].EdgeTo(FindClosestNodesOn(pathway, position)[1]), position);
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
        node1.Connect(node2, path.DistanceBetween(node1.Position(), node2.Position()), path);
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
        //TODO : Change return type to array
        List<Node> newNodes = new List<Node>(_nodes);
        newNodes.RemoveAll(n => path.DistanceBetween(n.Position(), position) < TOLERANCE);
        newNodes = newNodes.OrderBy(n => path.DistanceBetween(n.Position(), position)).ToList();
        if (newNodes.Count < 2) throw new ArgumentException("Only 1 or no node found on pathway. Must be at least 2");
        return new List<Node> { newNodes[0], newNodes[1] };
    }

    public Vector2 Orientation(Vector2 currentPos, Vector2 targetPos)
    {
        return _finalPath[0].Orientate(currentPos, targetPos, _direction);
    }
    
    /* Should be called when a cellulo leaves its current Pathway **/
    public void UpdateGraph()
    {
        
    }
    
    public void ComputePath(Vector2 current, Vector2 target)
    {
        Dictionary<Node, float> costSoFar = new Dictionary<Node, float>();
        Dictionary<Node, Node> comeFrom = new Dictionary<Node, Node>();
        Func<Node, float> sorter = n =>
        {
            float value = 0;
            costSoFar.TryGetValue(n, out value);
            return value;
        };
        PriorityList<Node> frontier = new PriorityList<Node>(sorter);
        Queue<Node> neighbors = new Queue<Node>();
        Node startNode = new Node(current);
        Node endNode = new Node(target);
        if (FindExistingNode(startNode).Equals(startNode)) AddNode(startNode.Position());
        if (FindExistingNode(endNode).Equals(endNode)) AddNode(endNode.Position());
        comeFrom.Add(startNode, null);

        //while (frontier.Count() > 0 || costSoFar.Count <= 2)
        while (frontier.Count() > 0)   // TODO : Try above version
        {
            Node currentNode = frontier.Peek(); 
            frontier.Remove();
            
            foreach (Node neighbor in currentNode.Neighbors())
            {
                //if (!IsBlocked(currentNode.EdgeTo(neighbor)) && !comeFrom.ContainsKey(neighbor))
                if (!comeFrom.ContainsKey(neighbor))  // TODO : Implement above version
                {
                    frontier.Add(neighbor);
                    comeFrom.Add(neighbor, currentNode);
                    
                    // TODO : Adapt for custom costs
                    costSoFar.Add(neighbor, neighbor.EdgeTo(currentNode).Length());
                }
            }
        }
        
        // If no path was found
        if (!comeFrom.ContainsKey(endNode))
        {
            // TODO : Check this case
        }
        
        // Builds the path starting from the end
        List<Node> reversePath = new List<Node> { endNode };
        Node tempNode = endNode;
        do
        {
            tempNode = GetFrom(comeFrom, tempNode);
            reversePath.Add(tempNode);
        } while (!IsNull(tempNode) && !IsNull(GetFrom(comeFrom, tempNode)));

        reversePath.Reverse();
        _finalNodes = reversePath;

        List<IPathway> pathways = new List<IPathway>();
        for (var i = 0; i < _nodes.Count - 1; i++)
        {
            pathways.Add(_finalNodes[i].EdgeTo(_finalNodes[i + 1]).Pathway());
        }
        _finalPath = pathways;
        
        
        /*
        frontier.Add(startNode.Neighbors());
        //_currentPathway = _map.FindClosestPathway(current);
        Node currentNode = frontier.Peek(); 
        frontier.Remove();
        costSoFar.Add(startNode, 0);
        
        costSoFar.Add(currentNode, currentNode.EdgeTo(startNode).Length()); //TODO : Update this to take cellulos into account
        comeFrom.Add(currentNode, startNode);
        foreach (Node node in frontier)
        {
            comeFrom.Add(node, startNode);
            costSoFar.Add(node, node.EdgeTo(startNode).Length());
        }
        
        // When all nodes have been reached
        if (frontier.IsEmpty() && costSoFar.Count > 2)
        {
            Node tempNode = endNode;
            _finalNodes.Add(endNode);
            _finalPath.Add(endNode.EdgeTo(GetFrom(comeFrom, endNode)).Pathway());
            do
            {
                _finalPath.Add(tempNode.EdgeTo(GetFrom(comeFrom, tempNode)).Pathway());
                tempNode = GetFrom(comeFrom, tempNode);
                _finalNodes.Add(tempNode);
            } while (!IsNull(GetFrom(comeFrom, GetFrom(comeFrom, tempNode))));

            Debug.Log($"Final path is {ListToString(_finalNodes)}");
            return new Path(current, target, _finalPath);
        }
        
        // When moved to next frontier pathfinding.Node
        if (neighbors.Count == 0)
        {
            currentNode = frontier.Peek();
            frontier.Remove();
            neighbors = new Queue<Node>(currentNode.Neighbors());
        }
        
        Node neighbor;
        do {
            neighbor = neighbors.Peek();
            neighbors.Clear();

        } while (neighbors.Count != 0 && costSoFar.ContainsKey(neighbor));
        
        if (!costSoFar.ContainsKey(neighbor)) {
            frontier.Add(neighbor);
            comeFrom.Add(neighbor, currentNode);
            AddNodeToCostSoFar(costSoFar, neighbor, currentNode);
            if (neighbor.Equals(endNode)) {
                Debug.Log("Final path computed");
                return;
            }
        }
        */

        
        
        throw new NotImplementedException();

    }

    private float AddNodeToCostSoFar(Dictionary<Node, float> costSoFar, Node node, Node previous)
    {
        float totalCost = node.EdgeTo(previous).Length();
        costSoFar.Add(node, totalCost);
        return totalCost;
    }

    public override string ToString()
    {
        Func<Node, string> formatter = n => $"{n.ToSimpleString()}";
        return $"Pathfinder [nodes = {ListToString(_nodes, formatter)}]";
    }

    public class Path
    {
        private List<IPathway> _pathways = new List<IPathway>();
        private Vector2 _position;
        private Vector2 _target;
        private Vector2 _direction;

        public Path(Vector2 startPos, Vector2 targetPos, List<IPathway> pathways)
        {
            _position = startPos;
            _target = targetPos;
            _pathways.AddRange(pathways);
        }
        
        public Vector2 GetOrientation()
        {
            return _pathways[0].Orientate(_position, _target, _direction);
        }
    }

    public class Edge
    {
        private Node _node1;
        private Node _node2;
        private float _length;
        private float _cost;
        private bool _isOccupied;
        private IPathway _pathway;

        public Edge(Node node1, Node node2, float length, IPathway path, float cost = BASE_COST, bool isOccupied = false)
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

        public IPathway Pathway() => _pathway;

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

        public Edge EdgeTo(Node node)
        {
            if (node.Equals(this)) throw new ArgumentException("This node and the target node must be different!");
            foreach (Edge edge in _edges)
            {
                if (edge.OtherNode(this).Equals(node)) return edge;
            }
            return null;
        }

        public ISet<Node> Neighbors() => new HashSet<Node>(_neighbors);

        public void Connect(Node node, float length, IPathway path, float cost = BASE_COST)
        {
            Connect(new Edge(this, node, length, path, cost));
        }
        
        public void Connect(Edge edge)
        {
            Node otherNode = edge.OtherNode(this);
            _edges.Add(edge);
            _neighbors.Add(otherNode);
            if (!otherNode.IsConnectedTo(this)) otherNode.Connect(edge);
        }

        /* Reconnects this node to another one that is in the middle of a given edge **/
        public void SplitConnection(Edge edge, Node newNode, float length, IPathway path, float cost = BASE_COST)
        {
            _neighbors.Remove(edge.OtherNode(this));
            _edges.Remove(edge);
            _edges.Add(new Edge(this, newNode, length, path, cost));
            _neighbors.Add(newNode);
        }

        public void Disconnect(Node node)
        {
            if (node._edges.Count != 2) throw new ArgumentException("Node must be an intermediary node!");
            Node node1 = node._edges.ToList()[0].OtherNode(this);
            Node node2 = node._edges.ToList()[1].OtherNode(this);
            throw new NotImplementedException();
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

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
    private const float TRIGGER_DIST = 0.01f;

    private readonly CircularMap _map;
    private ISet<Node> _nodes = new HashSet<Node>();
    private ISet<IPathway> _occupied = new HashSet<IPathway>();

    private Vector2 _direction;
    private Queue<Node> _finalNodes;
    private Queue<IPathway> _finalPath;
    
    /// You must generate a map before creating the pathfinder
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

    private void MergeNodes(Node node1, Node node2)
    {
        //TODO: Implement this if necessary
    }

    private Node AddNodeOnEdge(Edge edge, Vector2 newPosition)
    {
        Node newNode = new Node(newPosition);
        edge.Node1().Disconnect(edge.Node2());
        newNode.Connect(edge.Node1(), edge.Pathway().DistanceBetween(edge.Node1().Position(), newPosition), edge.Pathway());
        newNode.Connect(edge.Node2(), edge.Pathway().DistanceBetween(edge.Node2().Position(), newPosition), edge.Pathway());
        return newNode;
        //if (path.DistanceTo(edge.Node1().Position()) < path.DistanceTo(edge.Node2().Position()))
        /*
        IPathway path = edge.Pathway();
        if (IsNull(edge.Node1()) || IsNull(edge.Node2())) throw new ArgumentException("Edge is not connected to nodes!");
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

        _nodes.Add(keptNode);
        keptNode.SplitConnection(edge, new Node(newPosition), path.DistanceBetween(keptNode.Position(), newPosition), path);
    
        return keptNode;
        */
    }

    private Node AddNodeOnEdge(Vector2 newPosition)
    {
        return AddNodeOnEdge(FindClosestEdgeFrom(newPosition), newPosition);
        /*
        IPathway pathway = _map.FindClosestPathway(position);
        List<Node> nodes = FindClosestNodesOn(pathway, position);
        
        // TODO : Update this to allow for custom costs
        //nodes[0].Connect(nodes[1], pathway.DistanceBetween(nodes[0].Position(), nodes[1].Position()), pathway);
        nodes[0].Disconnect(nodes[1]);
        Edge edge = nodes[0].EdgeTo(nodes[1], position);
        if (edge == null || edge.Pathway().Equals(pathway)) throw new ArgumentException("Unexpected error happened. The graph seems a little different from the map");
        
        return AddNodeOnEdge(edge, position);
        */
    }

    /* Note : can only remove cellulo nodes, which are nodes that do not serve as a junction (i.e only have 2 edges) **/
    private void RemoveNode(IPathway path, Node node)
    {
        if (node.Edges().Count != 2)
            throw new ArgumentException(
                $"Can only remove edges that do not serve as a junction (i.e it mst have exactly 2 edges). " +
                $"It instead had {node.Edges().Count} edges");
        Node node1 = node.Edges().ToList()[0].OtherNode(node);
        Node node2 = node.Edges().ToList()[1].OtherNode(node);
        _nodes.Remove(node);
        node1.Connect(node2, path.DistanceBetween(node1.Position(), node2.Position()), path);
    }

    private bool NodeAlreadyExists(Node node)
    {
        foreach (Node possibleNode in _nodes)
        {
            if (node.Equals(possibleNode)) return true;
        }
        return false;
    }
    
    /** Returns the similar node already present when possible, otehrwise returns the given node */
    private Node FindExistingNode(Node toFind) => FindExistingNode(toFind, toFind);

    /** Returns the similar node already present when possible, otherwise returns a specified different Node */
    private Node FindExistingNode(Node toFind, Node ifNotFound)
    {
        foreach (Node possibleNode in _nodes)
        {
            if (toFind.Equals(possibleNode)) return possibleNode;
        }
        
        return ifNotFound;
    }

    private List<Node> FindClosestNodesOn(IPathway path, Vector2 position)
    {
        //TODO : Change return type to array
        List<Node> newNodes = new List<Node>(_nodes);
        newNodes.RemoveAll(n => path.DistanceFromPath(n.Position()) > TOLERANCE);
        newNodes = newNodes.OrderBy(n => path.DistanceBetween(n.Position(), position)).ToList();
        if (newNodes.Count < 2) throw new ArgumentException("Only 1 or no node found on pathway. Must be at least 2");
        return newNodes;
    }

    public Vector2 Orientation(Vector2 currentPos, Vector2 targetPos)
    {
        if (_finalNodes.Peek().IsCloseEnoughTo(targetPos) || _finalNodes.Count == 0)
        {
            return targetPos - currentPos;
        }
        if (_finalNodes.Peek().IsCloseEnoughTo(currentPos))
        {
            OnNextPathway();
        }
        return _finalPath.Peek().Orientate(currentPos, targetPos, _direction);
    }
    
    /// Should be called when another cellulo leaves its current Pathway
    public void UpdateGraph()
    {
        
    }

    public void OnNextPathway()
    {
        _finalPath.Clear();
        _finalNodes.Clear();
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
        Node startNode = FindExistingNode(new Node(current), null);
        Node endNode = FindExistingNode(new Node(target), null);
        bool isStartNodeNew = IsNull(startNode);
        bool isEndNodeNew = IsNull(endNode);
        if (isStartNodeNew) startNode = AddNodeOnEdge(current);
        if (isEndNodeNew) endNode = AddNodeOnEdge(target);
        
        costSoFar.Add(startNode, 0);
        frontier.Add(startNode.Neighbors());
        comeFrom.Add(startNode, startNode);
        
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
                    // TODO : Adapt for custom costs
                    costSoFar.Add(neighbor, neighbor.EdgeTo(currentNode).Length());
                    
                    frontier.Add(neighbor);
                    comeFrom.Add(neighbor, currentNode);
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
        } while (!IsNull(tempNode) && GetFrom(comeFrom, tempNode) != tempNode && !IsNull(GetFrom(comeFrom, tempNode)));

        reversePath.Reverse();
        _finalNodes = new Queue<Node>(reversePath);

        List<IPathway> pathways = new List<IPathway>();
        for (var i = 0; i < _finalNodes.Count - 1; i++)
        {
            pathways.Add(_finalNodes.Peek().EdgeTo(_finalNodes.ToList()[i + 1]).Pathway());
        }
        _finalPath = new Queue<IPathway>(pathways);
        
        // Removes the nodes of the cellulos
        if (isStartNodeNew) RemoveNode(_map.FindClosestPathway(startNode.Position()),startNode);
        if (isEndNodeNew) RemoveNode(_map.FindClosestPathway(endNode.Position()),endNode);
        
        
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

    private Edge FindClosestEdgeFrom(Vector2 position)
    {
        Edge closestEdge = null;
        float distance = float.MaxValue;
        foreach (Node node in _nodes)
        {
            foreach (Edge edge in node.Edges())
            {
                float newDistance = edge.Pathway().DistanceFromPath(position);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    closestEdge = edge;
                }
            }
        }
        return closestEdge;
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

    public ISet<Node> Nodes() => _nodes;
    
    public CircularMap Map() => _map;

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
            if (length <= 0) throw new ArgumentException($"Length must be greater than 0, but was {length}");
            _node1 = node1;
            _node2 = node2;
            _length = length;
            _pathway = path;
        }

        public ISet<Node> Nodes() => new HashSet<Node> { _node1, _node2 };
        
        public Node OtherNode(Node node) => _node1.Equals(node)? _node2: _node1;

        public float Length() => _length;

        public bool IsOccupied() => _isOccupied;

        public Node Node1() => _node1;

        public Node Node2() => _node2;

        public bool HasNode(Node node) => Nodes().Contains(node);

        public IPathway Pathway() => _pathway;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Edge)obj;
            return _node1.Equals(other._node1) && _node2.Equals(other._node2);
        }
        
        public override string ToString()
        {
            return $"Edge [node1 = {_node1.ToSimpleString()}, node2 = {_node2.ToSimpleString()}, length = {_length,3:F2}, cost = {_cost,3:F2}]";
        }
    }
    public class Node
    {
        private Vector2 _position;
        private ISet<Edge> _edges = new HashSet<Edge>();
        private ISet<Node> _neighbors = new HashSet<Node>();

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
        public bool IsConnectedTo(Node otherNode)
        {
            /*
            foreach (Edge edge in _edges)
            {
                if (edge.OtherNode(this).Equals(otherNode)) return true;
            }
            return false;
            */
            return _neighbors.Contains(otherNode);
        }

        private List<Edge> EdgesTo(Node node)
        {
            if (!_neighbors.Contains(node)) return null;
            if (node.Equals(this)) throw new ArgumentException("This node and the target node must be different!");
            
            //List<Edge> possibleEdges = _edges.ToList().FindAll(e => e.Pathway().IsOn(node._position));
            List<Edge> possibleEdges = _edges.ToList().FindAll(e => e.HasNode(node));
            return possibleEdges;
        }

        public Edge EdgeTo(Node node, Vector2 containing)
        {
            List<Edge> edges = EdgesTo(node);
            edges.RemoveAll(e => e.Pathway().IsOn(containing));
            if (edges.Count == 0) return null;
            if (edges.Count > 1) throw new Exception($"Duplicate edges found. Both contain the specified position {containing}. There may be more!");
            return edges[0];
        }
        
        public Edge EdgeTo(Node node)
        {
            List<Edge> edges = EdgesTo(node);
            if (IsNull(edges) || edges.Count == 0) return null;
            if (edges.Count > 1) throw new Exception("Duplicate edges. Try using the other method");
            return edges[0];
        }

        public bool IsCloseEnoughTo(Vector2 from) => Vector2.Distance(from, _position) < TRIGGER_DIST;

        public ISet<Node> Neighbors() => new HashSet<Node>(_neighbors);

        public void Connect(Node otherNode, float length, IPathway path, float cost = BASE_COST)
        {
            //Connect(new Edge(this, node, length, path, cost));
            
            // TODO : Modify this such that connecting with an already connected node rewrites the connection
            if (_neighbors.Contains(otherNode)) return;
            _neighbors.Add(otherNode);
            if (IsNull(EdgeTo(otherNode)) && IsNull(otherNode.EdgeTo(this)))
            {
                _edges.Add(new Edge(this, otherNode, length, path, cost));
            }

            if (!otherNode.IsConnectedTo(this))
            {
                otherNode.Connect(this, length, path, cost);
            }

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
            //if (node._edges.Count != 2) throw new ArgumentException("Node must be an intermediary node!");
            _edges.Remove(EdgeTo(node));
            _neighbors.Remove(node);
            
            if (IsConnectedTo(node))
            {
                node.Disconnect(this);
            }
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
            return $"Node [position = {_position}, neighbors = {ListToString(_neighbors, n => n.ToSimpleString())}]";
        }
    }
}

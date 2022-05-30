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
    private const float TRIGGER_DIST = 0.1f;
    private const float MERGE_DIST = 0.2f;

    private bool DEBUG_FAILED = false;
    
    private readonly CircularMap _map;
    private ISet<Node> _nodes = new HashSet<Node>();
    private ISet<Node> _initialNodes = new HashSet<Node>();
    private ISet<IPathway> _occupied = new HashSet<IPathway>();

    private Vector2 _targetPos;
    private Vector2 _currentPos;

    private Vector2 _direction;
    private Queue<Node> _finalNodes;
    private Queue<IPathway> _finalPath;

    private Node _startNode;
    private Node _endNode;
    
    /// You must generate a map before creating the pathfinder
    public Pathfinder(CircularMap map)
    {
        _map = map;
        _nodes = ReinitializeNodes();
    }

    public ISet<Node> ReinitializeNodes()
    {

        ISet<Node> newNodes = new HashSet<Node>();
        
        foreach (Passageway passage in _map.Passages())
        {
            Node node1 = FindExistingNode(new Node(passage.SmallPoint()), null, MERGE_DIST, newNodes);
            Node node2 = FindExistingNode(new Node(passage.LargePoint()), null, MERGE_DIST, newNodes);

            if (IsNull(node1)) {
                node1 = new Node(passage.SmallPoint());
                newNodes.Add(node1);
            }

            if (IsNull(node2)) {
                node2 = new Node(passage.LargePoint());
                newNodes.Add(node2);
            }
            node1.Connect(node2, passage.Length(), passage);
        }

        foreach (MapRing ring in _map.Rings())
        {
            List<Vector2> positions = _map.PassagesPointsOnRing(ring).ToList().OrderBy(pos => Angle(pos - ring.Center())).ToList();
            //List<Vector2> positions = new List<Node>(newNodes).RemoveAll(n => ring.DistanceFromPath(n.Position()) < TOLERANCE)

            Node current = null; Node next = null;
            if (positions.Count > 1)
            {
                
                Node first = GetExistingNode(positions[0], newNodes);
                for (int i = 0; i < positions.Count - 1; ++i)
                {
                    
                    current = GetExistingNode(positions[i], newNodes);
                    next = GetExistingNode(positions[i + 1], newNodes);
                    //if (!current.IsConnectedTo(next) && !current.Equals(next))
                    if (!current.IsConnectedTo(next) && current != next)
                    {
                        current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()), ring);
                    }
                }
                
                /*
                Node first = FindExistingNode(new Node(positions[0]), newNodes);
                for (int i = 0; i < positions.Count - 1; ++i)
                {
                    current = FindExistingNode(new Node(positions[i]), newNodes);
                    next = FindExistingNode(new Node(positions[i + 1]), newNodes);
                    if (!current.IsConnectedTo(next) && !current.Equals(next))
                    {
                        current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()), ring);
                    }
                }
                */
                
                next.Connect(first, ring.DistanceBetween(next.Position(), first.Position()), ring);
            }
        }

        _initialNodes = new HashSet<Node>(newNodes);

        return newNodes;
    }

    private Node GetExistingNode(Vector2 position, ICollection<Node> nodes)
    {
        List<Node> newNodes = new List<Node>(nodes);
        newNodes = newNodes.OrderBy(n => Vector2.Distance(n.Position(), position)).ToList();
        int count = newNodes.FindAll(n => Vector2.Distance(n.Position(), position) < MERGE_DIST).Count;
        if (count == 0) throw new ArgumentException($"There was no node at {position}! One should have been present there");
        if (count > 1) throw new ArgumentException($"Multiple nodes found at {position}! There should only be one");
        return newNodes[0];
    }

    private void MergeNodes(Node original, Node merged)
    {
        //TODO: Implement this if necessary
        foreach (Node neighbor in merged.Neighbors())
        {
            Edge edge = merged.EdgeTo(neighbor);
            original.Connect(neighbor, edge.Length(), edge.Pathway(), edge.Cost());
            merged.Disconnect(neighbor);
        }

        _nodes.Remove(merged);
    }

    private List<Node> FindBoundingNodesOn(IPathway path, Vector2 position, ICollection<Node> allNodes)
    {
        List<Node> nodes = FindNearestNodesOn(path, position, allNodes);
        //nodes = nodes.OrderBy(n => Vector2.SignedAngle(position - _map.Center(), n.Position() - _map.Center())).ToList();
        nodes = nodes.OrderBy(n => AngleBetween(position - _map.Center(), n.Position() - _map.Center())).ToList();
        Node node1 = nodes[0];
        Node node2 = nodes[nodes.Count - 1];
        //return new List<Node> { node1, node2 }.OrderBy(n => path.DistanceBetween(position, n.Position())).ToList();
        if (path.DistanceBetween(position, node1.Position()) < path.DistanceBetween(position, node2.Position()))
        {
            return new List<Node> { node1, node2 };
        }
        return new List<Node> { node2, node1 };
    }

    private Node InsertNode(IPathway path, Vector2 newPosition)
    {
        /*
        List<Node> nodes = FindNearestNodesOn(path, newPosition, _nodes);
        nodes = new List<Node> { nodes[0], nodes[1] };
        */
        List<Node> nodes = FindBoundingNodesOn(path, newPosition, _nodes);
        Edge edge = nodes[0].EdgeTo(nodes[1]);
        if (IsNull(edge) || IsNull(nodes[1].EdgeTo(nodes[0])))
        {
            nodes[0].Connect(nodes[1], path.DistanceBetween(nodes[0].Position(), nodes[1].Position()), path);
            edge = nodes[0].EdgeTo(nodes[1]);
            //throw new Exception($"Nodes {nodes[0]} and {nodes[1]} should be connected! There may be a problem with graph initialization");
            //DEBUG_FAILED = true;
        }
        Node newNode = new Node(newPosition);
        edge.Node1().Disconnect(edge.Node2());
        
        newNode.Connect(edge.Node1(), path.DistanceBetween(edge.Node1().Position(), newPosition), edge.Pathway());
        newNode.Connect(edge.Node2(), path.DistanceBetween(edge.Node2().Position(), newPosition), edge.Pathway());
        _nodes.Add(newNode);
        return newNode;
    }

    private IPathway MoveToNextPathway(IPathway nextPathway, Vector2 currentPos)
    {
        const float tolerance = 0.3f;
        List<IPathway> pathways = _map.Pathways().OrderBy(p => p.DistanceFromPath(currentPos)).ToList();
        if (pathways.Contains(nextPathway) && nextPathway.DistanceFromPath(currentPos) < tolerance)
        {
            return nextPathway;
        }

        return pathways[0];
    }

    /* Note : can only remove cellulo nodes, which are nodes that do not serve as a junction (i.e only have 2 edges) **/
    private void RemoveNode(Node node)
    {
        if (node.Edges().Count != 2)
        {
            return;
            //throw new ArgumentException($"Can only remove edges that do not serve as a junction (i.e it mst have exactly 2 edges). It instead had {node.Edges().Count} edges");
        }

        Node node1 = node.Edges().ToList()[0].OtherNode(node);
        Node node2 = node.Edges().ToList()[1].OtherNode(node);
        IPathway pathway = node.EdgeTo(node1).Pathway();
        if (!pathway.Equals(node.EdgeTo(node2).Pathway())) throw new Exception("Both edge connected to the removed node do not share the same pathway");

        node.Disconnect(node1);
        node.Disconnect(node2);
        _nodes.Remove(node);
        
        //TODO : Adapt this to allow for custom node cost
        // TODO : DEBUG
        float length = pathway.DistanceBetween(node1.Position(), node2.Position());
        node1.Connect(node2, pathway.DistanceBetween(node1.Position(), node2.Position()), pathway);
    }

    /** Returns the similar node already present when possible, otehrwise returns the given node */
    private Node FindExistingNode(Node toFind, ICollection<Node> nodes) => FindExistingNode(toFind, toFind, nodes);

    private Node FindExistingNode(Node toFind, Node ifNotFound, ICollection<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            if (toFind.Equals(node)) return node;
        }

        return ifNotFound;
    }
    
    /** Returns the similar node already present when possible, otherwise returns a specified different Node */
    private Node FindExistingNode(Node toFind, Node ifNotFound, float minDistance, ICollection<Node> nodes)
    {
        float smallestDistance = float.MaxValue;
        Node foundNode = null;
        foreach (Node possibleNode in nodes)
        {
            float currentDistance = Vector2.Distance(toFind.Position(), possibleNode.Position());
            if (currentDistance < smallestDistance && currentDistance < minDistance)
            {
                smallestDistance = currentDistance;
                foundNode = possibleNode;
            }
        }
        
        return foundNode == null ? ifNotFound : foundNode;
    }

    private List<Node> FindNearestNodesOn(IPathway path, Vector2 position, ICollection<Node> nodes)
    {
        //TODO : Change return type to array
        
        ISet<Node> newNodes = new HashSet<Node>();
        foreach (Node node in nodes)
        {
            foreach (Edge edge in node.Edges())
            {
                if (edge.Pathway().Equals(path)) newNodes.Add(node);
            }
        }
        if (newNodes.Count < 2) throw new ArgumentException("Only 1 or no node found on pathway. Must be at least 2");
        return newNodes.OrderBy(n => path.DistanceBetween(n.Position(), position)).ToList();
    }

    public Vector2 Orientation(Vector2 currentPos, Vector2 targetPos)
    {

        if (_finalNodes.Count == 0) return new Vector2(0, 0);
        Node nextNode = _finalNodes.Peek();
        IPathway nextPath = _finalPath.Peek();
        //if (nextNode.IsCloseEnoughTo(targetPos) || _finalNodes.Count == 0) return targetPos - currentPos;

        //if (nextWaypointNode.IsCloseEnoughTo(currentPos)) OnNextPathway();

        if (Vector2.Distance(currentPos, nextNode.Position()) < TRIGGER_DIST) OnNextPathway();

        Vector2 newDirection = nextPath.Orientate(currentPos, nextNode.Position());
        Vector2 mathDirection = nextPath.Orientate(currentPos, targetPos );
        
        /*
        Debug.Log("-------------------------------");
        Debug.Log($"Waypoints : {ListToString(_finalNodes.ToList())}");
        Debug.Log($"Pathways : {ListToString(_finalPath.ToList())}");
        Debug.Log($"Direction : {mathDirection}");
        Debug.Log($"Angle : {Angle(currentPos - _map.Center())}");
        Debug.Log($"Distance to next waypoint : {nextPath.DistanceBetween(currentPos, nextNode.Position())}");
        Debug.Log($"Next path : {nextPath}");
        Debug.Log($"Next node : {nextNode}");
        */
        
        
        return newDirection;
    }

    public void OnNextPathway()
    {
        _finalPath.Dequeue();
        _finalNodes.Dequeue();
    }
    
    /// <summary>
    ///     Sets the path from the current position to a specified target
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="target"></param>
    /// <exception cref="Exception"></exception>
    public void GoToTarget(Vector2 currentPos, Vector2 target)
    {
        //_nodes = ReinitializeNodes();
        _targetPos = target;
        _currentPos = currentPos;

        // ------ TODO : DEBUG ----------
        /*
        if (Time.time > 0.1f)
        {
            Debug.Log($"Position = {currentPos}, Target = {target}");
            Debug.Log($"Path, Nodes : {ListToString(_finalNodes.ToList())}");
            Debug.Log($"Path, pathways : {ListToString(_finalPath.ToList())}");
        }
        Debug.Log(DrawGraph());
        */
        
        Dictionary<Node, float> costSoFar = new Dictionary<Node, float>();
        Dictionary<Node, Node> comeFrom = new Dictionary<Node, Node>();
        PriorityList<Node> frontier = new PriorityList<Node>(n => GetFrom(costSoFar, n));
        Node startNode = FindExistingNode(new Node(currentPos), null, MERGE_DIST, _nodes);
        Node endNode = FindExistingNode(new Node(target), null, MERGE_DIST, _nodes);
        bool isStartNodeNew = IsNull(startNode);
        bool isEndNodeNew = IsNull(endNode);
        if (isStartNodeNew) {
            //startNode = InsertNode(_map.FindClosestPathway(currentPos), currentPos);
            
            if (!IsNull(_finalPath) && _finalPath.Count > 1) {
                //startNode = InsertNode(MoveToNextPathway(_finalPath.ToList()[1], currentPos), currentPos);
                startNode = InsertNode(_map.FindClosestPathway(currentPos), currentPos);
            } else {
                startNode = InsertNode(_map.FindClosestPathway(currentPos), currentPos);
            }
            
        }

        if (isEndNodeNew) {
            endNode = InsertNode(_map.FindClosestPathway(target), target);
        }

        _startNode = startNode;
        _endNode = endNode;

        costSoFar.Add(startNode, 0);
        //frontier.Add(startNode.Neighbors());
        frontier.Add(startNode);
        comeFrom.Add(startNode, startNode);

        //while (frontier.Count() > 0 || costSoFar.Count <= 2)
        //while (!reachedTarget && frontier.Count() > 0)  
        while (frontier.Count() > 0)   // TODO : Try above version
        {
            Node currentNode = frontier.Peek(); 
            frontier.Dequeue();

            if (currentNode.Equals(endNode)) break;
            
            //PriorityList<Node> neighbors = new PriorityList<Node>(currentNode.Neighbors(), n => GetFrom(costSoFar, n));
            foreach (Node neighbor in currentNode.Neighbors())
            //while (neighbors.Count() > 0)
            {
                //Node neighbor = neighbors.Peek();
                //neighbors.Dequeue();
                //if (!IsBlocked(currentNode.EdgeTo(neighbor)) && !comeFrom.ContainsKey(neighbor))
                float newCost = GetFrom(costSoFar, currentNode) + currentNode.EdgeTo(neighbor).Length();
                //if (!comeFrom.ContainsKey(neighbor))  // TODO : Implement above version
                if (!comeFrom.ContainsKey(neighbor) || newCost < GetFrom(costSoFar, neighbor))  // TODO : Implement above version
                {
                    // TODO : Adapt for custom costs
                    //costSoFar.Add(neighbor, neighbor.EdgeTo(currentNode).Length());
                    //costSoFar.Add(neighbor, newCost);

                    if (costSoFar.ContainsKey(neighbor)) costSoFar.Remove(neighbor);
                    costSoFar.Add(neighbor, newCost);
                    
                    
                    if (comeFrom.ContainsKey(neighbor)) comeFrom.Remove(neighbor);
                    comeFrom.Add(neighbor, currentNode);
                    
                    frontier.Add(neighbor);

                    //if (currentNode.Equals(endNode)) reachedTarget = true;
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
        } while (!(IsNull(tempNode) || GetFrom(comeFrom, tempNode).Equals(tempNode) || IsNull(GetFrom(comeFrom, tempNode))));
        //($"Final node (--> path start node) is {tempNode} and isStartNodeNew is {isStartNodeNew}");
        
        reversePath.Reverse();
        _finalNodes = new Queue<Node>(reversePath);

        // Builds the final path
        List<IPathway> pathways = new List<IPathway>();
        List<Node> tempNodes = _finalNodes.ToList();
        for (var i = 0; i < _finalNodes.Count - 1; i++)
        {
            //pathways.Add(_finalNodes.Peek().EdgeTo(_finalNodes.ToList()[i + 1]).Pathway());
            pathways.Add(tempNodes[i].EdgeTo(tempNodes[i+1]).Pathway());
        }
        _finalPath = new Queue<IPathway>(pathways);
        //if (!isStartNodeNew) _finalNodes.Dequeue();  // Removes the start node from the list of waypoints
        _finalNodes.Dequeue();  // Removes the start node from the list of waypoints

        /*
        if (_finalNodes.ToList()[1].Equals(endNode) && !isStartNodeNew && !isEndNodeNew)
        {
            _finalNodes.Dequeue();
            _finalPath.Dequeue();
        }
        */
        
        
        _nodes = ReinitializeNodes();
        
        // Removes the nodes of the cellulos
        //if (isStartNodeNew) RemoveNode(_map.FindClosestPathway(startNode.Position()),startNode);
        //if (isStartNodeNew) RemoveNode(startNode);
        //if (isEndNodeNew) RemoveNode(_map.FindClosestPathway(endNode.Position()),endNode);
        //if (isEndNodeNew) RemoveNode(endNode);

    }

    public float DistanceToClosestNode(Vector2 from)
    {
        float distance = float.MaxValue;
        foreach (Node node in _nodes)
        {
            float newDistance = Vector2.Distance(from, node.Position());
            if (newDistance < distance) distance = newDistance;
        }
        return distance;
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
        return $"Pathfinder [nodes = {ListToString(_nodes)}]";
    }
    
    [Obsolete("----- For debugging only! -----")]
    public Node FindNode(float x, float y)
    {
        Vector2 position = new Vector2(x, y);
        Node node = MinElement(_nodes, n => Vector2.Distance(n.Position(), position));
        if (Vector2.Distance(node.Position(), position) > 0.3f) return null;
        return node;
    }
    
    [Obsolete("----- For debugging only! -----")]
    public string DrawGraph()
    {
        if (Nodes().Count == 0) return "Graph[EMPTY]";
        List<Node> nodes = Nodes().OrderBy(n => Vector2.Distance(n.Position(), Map().Center())).ToList();

        float radius = Vector2.Distance(nodes[0].Position(), Map().Center());
        string str = $"\nRing with radius {radius} : ";
        int ringID = 0;
        foreach (Node node in nodes)
        {
            float newRadius = Vector2.Distance(node.Position(), Map().Center());
            if (newRadius > radius + CircularMap.MARGIN)
            {
                radius = newRadius;
                ++ringID;
                if (str.Length >= 2) str = str.Remove(str.Length - 2, 2);
                str += $"\nRing with radius {radius} : ";
            }

            str = str + "\n  -> " + node.ToFullString() + ", ";
        }
        
        str = str.Remove(str.Length - 2, 2);
        str += $"\nPlayer position : {GameObject.FindGameObjectWithTag("Player").transform.localPosition}";
        str += $"\nGhost position : {GameObject.FindGameObjectWithTag("Ghost").transform.localPosition}";
        return $"Graph [{str}\n]";
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
            if (IsNull(node1) || IsNull(node2)) throw new ArgumentException($"Nodes can't be null! Nodes were {node1} and {node2}");
            if (node1.Equals(node2)) throw new ArgumentException("Nodes must be different from one another!");
            //if (length <= 0) throw new ArgumentException($"Length must be greater than 0, but was {length}");
            if (length < 0) throw new ArgumentException($"Length must be greater or equal to 0, but was {length}");
            _node1 = node1;
            _node2 = node2;
            _length = length;
            _pathway = path;
        }

        public ISet<Node> Nodes() => new HashSet<Node> { _node1, _node2 };
        
        public Node OtherNode(Node node) => _node1.Equals(node)? _node2: _node1;

        public float Length() => _length;

        public float Cost() => _cost;

        public bool IsOccupied() => _isOccupied;

        public Node Node1() => _node1;

        public Node Node2() => _node2;

        public bool HasNode(Node node) => Nodes().Contains(node);

        public IPathway Pathway() => _pathway;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Edge)obj;
            bool sameNodes = (_node1.Equals(other._node1) && _node2.Equals(other._node2))
                              || (_node1.Equals(other._node2) && _node2.Equals(other._node1));
            return  sameNodes && AreSame(_length, other._length) && AreSame(_cost, other._cost) 
                    && _isOccupied == other._isOccupied && _pathway.Equals(other._pathway);
        }
        
        public override string ToString()
        {
            return $"Edge [node1 = {_node1}, node2 = {_node2}, length = {_length,3:F2}, cost = {_cost,3:F2}]";
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
            if (otherNode == this) throw new ArgumentException("Can't connect a node to itself!");
            if (length < 0) throw new ArgumentException("Length must be greater or equal to zero!");
            
            // TODO : Modify this such that connecting with an already connected node rewrites the connection
            if (!_neighbors.Contains(otherNode)) _neighbors.Add(otherNode);
            
            if (IsNull(EdgeTo(otherNode)))
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
            
            if (node.IsConnectedTo(this))
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

        public string ToFullString()
        {
            return $"Node [position = {_position}, neighbors = {ListToString(_neighbors)}]";
        }

        public override string ToString()
        {
            return $"Node [position = {_position}]";
        }
    }
}

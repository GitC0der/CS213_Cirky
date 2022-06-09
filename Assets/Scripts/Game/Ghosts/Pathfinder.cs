using static Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Behaviors;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using MapRing = CircularMap.MapRing;
using Passageway = CircularMap.Passageway;
using IPathway = CircularMap.IPathway;
using Random = UnityEngine.Random;


namespace Game.Ghosts{

/// <summary>
///     Steps to use the pathfinder as intended :
///     <list type="number">
///         <item><term> Create a CircularMap and then initialize a pathfinder with its constructor </term></item>
///         <item><term> If obstacles such as other cellulos or objects are present, pass their GameObject to the pathfinder constructor </term></item>
///         <item><term> Use the <see cref="SetTarget(Vector2, Vector2)">SetTarget()</see> method to set the target of the pathfinder </term></item>
///         <item><term> Then use the <see cref="Orientation(Vector2)">Orientation()</see> method to get the orientation (i.e normalized velocity) of the cellulo </term></item>
///     </list>
///     See <see cref="GhostBehavior">GhostBehavior</see> for more information and examples
/// </summary>
public class Pathfinder
{
    private const float OBSTACLE_CLEARANCE = 1.35f*CircularMap.MARGIN;
    private const float BASE_COST = 1;
    public const float TRIGGER_DIST = 0.4f;
    private const float MERGE_DIST = 0.2f;

    public static bool GHOST_IS_BLOCKING = true;
    
    private readonly CircularMap _map;
    private ISet<Node> _nodes;

    private readonly GhostBehavior _owner;
    private bool _frozen;
    private bool _isWaiting;
    private Node _endNode;
    private Queue<Node> _finalNodes;
    private Queue<IPathway> _finalPath;
    private float _distanceToTarget;
    private List<GameObject> _obstacles;
    private IList<Node> _obstacleNodes;

    /// *** You must generate a map before creating the pathfinder! ***
    public Pathfinder(CircularMap map, GhostBehavior owner) : this(map, new List<GameObject>(), owner) {}
    
    /// *** You must generate a map before creating the pathfinder! ***
    /// This constructor asks for obstacles, i.e other cellulos or objects that could block the path of this cellulo
    public Pathfinder(CircularMap map, ICollection<GameObject> obstacles, GhostBehavior owner)
    {
        _map = map;
        _nodes = ResetNodes();
        _obstacles = new List<GameObject>(obstacles);
        _owner = owner;
    }

    /// Recomputes the nodes of the map alone, i.e without the start and end nodes 
    private ISet<Node> ResetNodes()
    {
        ISet<Node> newNodes = new HashSet<Node>();
        foreach (Passageway passage in _map.Passages())
        {
            Node node1 = FindExistingNode(new Node(passage.SmallPoint(), false), null, MERGE_DIST, newNodes);
            Node node2 = FindExistingNode(new Node(passage.LargePoint(), false), null, MERGE_DIST, newNodes);

            if (IsNull(node1)) {
                node1 = new Node(passage.SmallPoint(), false);
                newNodes.Add(node1);
            }

            if (IsNull(node2)) {
                node2 = new Node(passage.LargePoint(), false);
                newNodes.Add(node2);
            }
            node1.Connect(node2, passage.Length(), passage);
        }

        foreach (MapRing ring in _map.Rings())
        {
            List<Vector2> positions = _map.PassagesPointsOnRing(ring).ToList().OrderBy(pos => Angle(pos - ring.Center())).ToList();

            Node current = null; Node next = null;
            if (positions.Count > 1)
            {
                
                Node first = GetExistingNode(positions[0], newNodes);
                for (int i = 0; i < positions.Count - 1; ++i)
                {
                    
                    current = GetExistingNode(positions[i], newNodes);
                    next = GetExistingNode(positions[i + 1], newNodes);
                    if (!current.IsConnectedTo(next) && current != next)
                    {
                        current.Connect(next, ring.DistanceBetween(current.Position(), next.Position()), ring);
                    }
                }
                
                next.Connect(first, ring.DistanceBetween(next.Position(), first.Position()), ring);
            }
        }
        
        return newNodes;
    }

    public void SetObstacles(ICollection<GameObject> newObstacles)
    {
        _obstacles = newObstacles.ToList();
    }

    public void RemoveObstacles(ICollection<GameObject> removedObstacles)
    {
        foreach (GameObject obstacle in removedObstacles)
        {
            if (_obstacles.Contains(obstacle)) _obstacles.Remove(obstacle);
        }
    }

    /// Gets the node at a specified position. Note : there must be one and only one node at the position!
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

    /// Finds the 2 nodes on a path that are the closest nodes surrounding a specified position 
    private List<Node> FindBoundingNodesOn(IPathway path, Vector2 position, ICollection<Node> allNodes)
    {
        List<Node> nodes = FindNodesOn(path, allNodes);
        if (nodes.Count < 2) throw new Exception($"Pathway {path} should contain at least 2 nodes, instead contained {nodes.Count}");
        nodes = nodes.OrderBy(n => AngleBetween(position - _map.Center(), n.Position() - _map.Center())).ToList();
        Node node1 = nodes[0];
        Node node2 = nodes[nodes.Count - 1];
        if (path.DistanceBetween(position, node1.Position()) < path.DistanceBetween(position, node2.Position()))
        {
            return new List<Node> { node1, node2 };
        }
        return new List<Node> { node2, node1 };
    }

    /// Inserts a new node on a pathway
    private Node InsertNode(IPathway path, Vector2 newPosition, bool isBlocking)
    {
        List<Node> nodes = FindBoundingNodesOn(path, newPosition, _nodes);
        Edge edge = nodes[0].EdgeTo(nodes[1]);
        if (IsNull(edge) || IsNull(nodes[1].EdgeTo(nodes[0])))
        {
            // TODO : May prove to cause problems. If true, revert back to resetting the nodes after each frame
            nodes[0].Connect(nodes[1], path.DistanceBetween(nodes[0].Position(), nodes[1].Position()), path);
            edge = nodes[0].EdgeTo(nodes[1]);
            Debug.Log($"Warning! {nodes[0]} and {nodes[1]} were not connected. Correction applied... May need to investigate this later on");
            //throw new Exception($"Nodes {nodes[0]} and {nodes[1]} should be connected! There may be a problem with graph initialization");
        }
        Node newNode = new Node(newPosition, isBlocking);
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

    /// Note : can only remove cellulo nodes, which are nodes that do not serve as a junction (i.e only have 2 edges) 
    private void RemoveNode(Node node)
    {
        if (node.Edges().Count != 2)
        {
            //return;
            throw new ArgumentException($"Can only remove edges that do not serve as a junction (i.e it mst have exactly 2 edges). It instead had {node.Edges().Count} edges");
        }

        Node node1 = node.Edges().ToList()[0].OtherNode(node);
        Node node2 = node.Edges().ToList()[1].OtherNode(node);
        IPathway pathway = node.EdgeTo(node1).Pathway();
        if (!pathway.Equals(node.EdgeTo(node2).Pathway())) throw new Exception("Both edge connected to the removed node do not share the same pathway");

        node.Disconnect(node1);
        node.Disconnect(node2);
        _nodes.Remove(node);
        
        // TODO : Adapt this to allow for custom node cost
        node1.Connect(node2, pathway.DistanceBetween(node1.Position(), node2.Position()), pathway);
    }

 
    /// Returns the similar node already present when possible, otherwise returns the given node
    private Node FindExistingNode(Node toFind, Node ifNotFound, ICollection<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            if (toFind.Equals(node)) return node;
        }

        return ifNotFound;
    }
    
    /// Returns the similar node already present when possible, otherwise returns a specified different Node
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

    private List<Node> FindNodesOn(IPathway path, ICollection<Node> nodes)
    {
        ISet<Node> newNodes = new HashSet<Node>();
        foreach (Node node in nodes)
        {
            foreach (Edge edge in node.Edges())
            {
                if (edge.Pathway().Equals(path)) newNodes.Add(node);
            }
        }
        if (newNodes.Count < 2) throw new ArgumentException("Only 1 or no node found on pathway. Must be at least 2");
        //return newNodes.OrderBy(n => path.DistanceBetween(n.Position(), position)).ToList();
        return newNodes.ToList();
    }

    /// <summary>
    ///     Computes the (normalized) orientation of the cellulo and removes nodes from the path as the cellulo reaches them
    /// </summary>
    /// <param name="currentPos">The current position of the cellulo</param>
    /// <returns>The (normalized) orientation of the cellulo</returns>
    public Vector2 Orientation(Vector2 currentPos, bool isFleeing)
    {
        // TODO : Might need a better method in case of larger obstacles
        GhostBehavior otherGhost = GameManager.Instance.ClosestGhostTo(currentPos, true).GetComponent<GhostBehavior>();
        bool closeToGhost = Vector2.Distance(ToVector2(otherGhost.transform.localPosition), currentPos) < OBSTACLE_CLEARANCE;
        bool otherIsWaiting = otherGhost.IsWaiting();
        if (closeToGhost && !otherIsWaiting) {
            //if (_distanceToTarget > otherGhost.DistanceToTarget()) _isWaiting = true;
            _isWaiting = !otherGhost.IsWaiting();
            if (!isFleeing)
            {
                _isWaiting = _distanceToTarget > otherGhost.DistanceToTarget();
            }
        } else {
            _isWaiting = false;
        } 
        
        if (_frozen || _isWaiting) return new Vector2(0, 0);
        
        /*
        if (_finalNodes.Count == 0 && !isFleeing) return new Vector2(0, 0);

        if (_finalNodes.Count == 0 && isFleeing) GenerateFleeingTarget(currentPos);
        */

        Node nextNode = _finalNodes.Peek();
        IPathway nextPath = _finalPath.Peek();
        
        /*
        if (Vector2.Distance(currentPos, nextNode.Position()) < TRIGGER_DIST)
        {
            _finalPath.Dequeue();
            _finalNodes.Dequeue();
        }
        */

        Vector2 newDirection = nextPath.Orientate(currentPos, nextNode.Position());

        return newDirection;
    }
    
    public Vector2 GenerateFleeingTarget(Vector2 currentPos)
    {
        float MIN_DISTANCE = 7f;
        List<GameObject> objects = GameObject.FindGameObjectsWithTag("Ghost").ToList();
        objects.Add(GameManager.Instance.Player().gameObject);
        
        float length = 0;
        bool isOccupied;
        Vector2 position;
        do
        {
            isOccupied = false;
            position = _map.RandomPosition();
            foreach (GameObject ghost in objects)
            {
                isOccupied = isOccupied || position.Equals(ToVector2(ghost.transform.localPosition));
            }

            //if (!isOccupied) length = SetTarget(currentPos, position, true);
            if (!isOccupied) length = DistanceBetween(currentPos, position, true);
        } while (isOccupied || length < MIN_DISTANCE);

        return position;
    }

    public float SetFleeing(Vector2 currentPos, Vector2 currentDirection, Vector2 previousPos)
    {
        // TODO : Modularize this
        _frozen = false;
        _obstacleNodes = new List<Node>();
        //Node previousNode = MinElement(_nodes, n => Vector2.Distance(n.Position(), _endNode.Position()));
        
        
        // Merge the start node with a existing node if there is one, otherwise creates a new node
        Node startNode = FindExistingNode(new Node(currentPos, false), null, MERGE_DIST, _nodes);
        bool isStartNodeNew = IsNull(startNode);
        if (isStartNodeNew) {
            startNode = InsertNode(_map.FindClosestPathway(currentPos), currentPos, GHOST_IS_BLOCKING);
        }

        //if (previousPos.Equals(startNode.Position())) return _distanceToTarget;
        
        Vector2 playerPos = ToVector2(GameManager.Instance.Player().gameObject.transform.localPosition);
        Node playerNode = FindExistingNode(new Node(playerPos, true), null, MERGE_DIST, _nodes);
        bool isPlayerNodeNew = IsNull(playerNode);
        if (isPlayerNodeNew)
        {
            playerNode = InsertNode(_map.FindClosestPathway(playerPos), playerPos, GHOST_IS_BLOCKING);
        }
        
        // Adds nodes at obstacles
        foreach (GameObject gameObject in _obstacles)
        {
            Vector2 obstaclePos = ToVector2(gameObject.transform.localPosition);
            Node obstacleNode = FindExistingNode(new Node(obstaclePos, true), null,
                MERGE_DIST, _nodes);
            if (IsNull(obstacleNode))
            {
                _obstacleNodes.Add(InsertNode(_map.FindClosestPathway(obstaclePos), obstaclePos, true));
            }
        }

        
        ISet<Node> possibleNodes = startNode.Neighbors();
        if (possibleNodes.Contains(playerNode)) possibleNodes.Remove(playerNode);
        Node removedNode = null;
        //if (possibleNodes.Contains(previousNode)) possibleNodes.Remove(previousNode);

        foreach (Node obstacleNode in _obstacleNodes)
        {
            if (possibleNodes.Contains(obstacleNode)) possibleNodes.Remove(obstacleNode);
        }
        Debug.Log($"{_owner} : Possible nodes are {ListToString(possibleNodes)}, player node is {playerNode}");

        
        if (!currentDirection.Equals(default))
        {
            foreach (Node node in new List<Node>(possibleNodes))
            {
                IPathway pathway = startNode.EdgeTo(node).Pathway();
                //float angle = AngleBetween(currentDirection, pathway.Orientate(startNode.Position(), node.Position()));
                float angle = Vector2.Angle(currentDirection, pathway.Orientate(startNode.Position(), node.Position()));
                Debug.Log($"{_owner} : Angle is {angle}");
                Debug.Log($"Current direction is {currentDirection}");
                if (angle < 30)
                {
                    removedNode = node;
                    possibleNodes.Remove(node);
                }
            }
        }
        


        // If no available node
        if (possibleNodes.Count == 0)
        {
            /*
            _endNode = _obstacleNodes.Contains(previousNode)
                ? _endNode = _obstacleNodes[Random.Range(0, possibleNodes.Count)]
                : previousNode;
                */
            if (removedNode == null)
            {
                possibleNodes = startNode.Neighbors();
                if (possibleNodes.Contains(playerNode)) possibleNodes.Remove(playerNode);
                _endNode = possibleNodes.ToList()[Random.Range(0, possibleNodes.Count)];
            }
            else
            {
                _endNode = removedNode;
            }
            
        }
        else
        {
            _endNode = possibleNodes.ToList()[Random.Range(0, possibleNodes.Count)];
        }

        if (isStartNodeNew) _nodes.Remove(startNode);
        if (isPlayerNodeNew) _nodes.Remove(playerNode);
        
        _finalNodes = new Queue<Node>();
        _finalNodes.Enqueue(_endNode);
        _finalPath = new Queue<IPathway>();
        _finalPath.Enqueue(startNode.EdgeTo(_endNode).Pathway());
        _distanceToTarget = startNode.EdgeTo(_endNode).Length();

        
        _obstacles.Remove(GameManager.Instance.Player().gameObject);
        return _distanceToTarget;
    }

    
    /// <summary>
    ///     Sets the path from the current position to a specified target
    /// </summary>
    /// <param name="currentPos">Current position of the cellulo</param>
    /// <param name="target">Target position where the cellulo will move towards</param>
    /// <param name="avoidPlayer">True if the cellulo flees the player, false if not</param>
    /// <return> The length of the path </return>
    /// <exception cref="Exception">If the map is not correctly initialized</exception>
    public float SetTarget(Vector2 currentPos, Vector2 target, bool avoidPlayer)
    {
        _frozen = false;
        Dictionary<Node, float> costSoFar = new Dictionary<Node, float>();
        Dictionary<Node, Node> comeFrom = new Dictionary<Node, Node>();
        PriorityList<Node> frontier = new PriorityList<Node>(n => GetFrom(costSoFar, n));
        _obstacleNodes = new List<Node>();
        if (avoidPlayer) _obstacles.Add(GameManager.Instance.Player().gameObject);
        
        // Merge the start and end node with a existing node if there is one, otherwise creates a new node
        Node startNode = FindExistingNode(new Node(currentPos, false), null, MERGE_DIST, _nodes);
        Node endNode = FindExistingNode(new Node(target, false), null, MERGE_DIST, _nodes);
        bool isStartNodeNew = IsNull(startNode);
        bool isEndNodeNew = IsNull(endNode);
        if (isStartNodeNew) {
            startNode = InsertNode(_map.FindClosestPathway(currentPos), currentPos, GHOST_IS_BLOCKING);
        }
        if (isEndNodeNew) {
            endNode = InsertNode(_map.FindClosestPathway(target), target, false);
        }
        
        // Generates obstacle nodes
        foreach (GameObject gameObject in _obstacles)
        {
            Vector2 obstaclePos = ToVector2(gameObject.transform.localPosition);
            Node obstacleNode = FindExistingNode(new Node(obstaclePos, true), null,
                MERGE_DIST, _nodes);
            if (IsNull(obstacleNode))
            {
                _obstacleNodes.Add(InsertNode(_map.FindClosestPathway(obstaclePos), obstaclePos, true));
            }
        }

        costSoFar.Add(startNode, 0);
        frontier.Add(startNode);
        comeFrom.Add(startNode, startNode);
        
        // Searches the graph
        while (frontier.Count() > 0)
        {
            Node currentNode = frontier.Peek(); 
            frontier.Dequeue();

            if (currentNode.Equals(endNode)) break;
            
            foreach (Node neighbor in currentNode.Neighbors())
            {
                // If the neighboring node is blocked, it is completely avoided
                if (!neighbor.IsBlocking())
                {
                    float newCost = GetFrom(costSoFar, currentNode) + currentNode.EdgeTo(neighbor).Length();
                    if (!comeFrom.ContainsKey(neighbor) || newCost < GetFrom(costSoFar, neighbor))
                    {
                        // TODO : Adapt for custom costs
                        if (costSoFar.ContainsKey(neighbor)) costSoFar.Remove(neighbor);
                        costSoFar.Add(neighbor, newCost);

                        if (comeFrom.ContainsKey(neighbor)) comeFrom.Remove(neighbor);
                        comeFrom.Add(neighbor, currentNode);

                        frontier.Add(neighbor);
                    }
                }
            }
        }
        
        // If no path was found
        if (!comeFrom.ContainsKey(endNode))
        {
            _finalNodes = new Queue<Node>(new List<Node> {startNode});
            _finalPath = new Queue<IPathway>(new List<IPathway> { _map.FindClosestPathway(currentPos) });
            _nodes = ResetNodes();
            return 0;
        }

        // Builds the path starting from the end
        List<Node> reversePath = new List<Node> { endNode };
        Node tempNode = endNode;
        do
        {
            tempNode = GetFrom(comeFrom, tempNode);
            reversePath.Add(tempNode);
        } while (!(IsNull(tempNode) || GetFrom(comeFrom, tempNode).Equals(tempNode) || IsNull(GetFrom(comeFrom, tempNode))));
        
        reversePath.Reverse();
        _finalNodes = new Queue<Node>(reversePath);

        // Builds the final path
        List<IPathway> pathways = new List<IPathway>();
        List<Node> tempNodes = _finalNodes.ToList();
        for (var i = 0; i < _finalNodes.Count - 1; i++)
        { 
            pathways.Add(tempNodes[i].EdgeTo(tempNodes[i + 1]).Pathway());
        }
        _finalPath = new Queue<IPathway>(pathways);
        _endNode = endNode;

        // Removes the added start, end nodes, and obstacle nodes from the graph
        if (isStartNodeNew) RemoveNode(startNode);
        if (isEndNodeNew) RemoveNode(endNode);
        foreach (Node obstacleNode in _obstacleNodes)
        {
            RemoveNode(obstacleNode);
        }
        if (avoidPlayer) _obstacles.Remove(GameManager.Instance.Player().gameObject);

        // Computes the length of the path
        float length = 0;
        List<Node> lengthNodes = _finalNodes.ToList();
        List<IPathway> lengthPathways = _finalPath.ToList();
        for (int i = 0; i < _finalPath.Count; i++)
        {
            // TODO : Use edges instead
            length += lengthPathways[i].DistanceBetween(lengthNodes[i].Position(), lengthNodes[i + 1].Position());
        }
        
        // Removes the start node from the list of waypoints since the start node is the current position
        _finalNodes.Dequeue();

        _distanceToTarget = length;
        return length;
    }

    /// Freezes the cellulo in its current position
    public void Freeze()
    {
        _frozen = true;
    }

    public Vector2 CurrentTarget() => _endNode.Position();

    /// Returns true if the cellulo is waiting for another cellulo to move out of the way, false otherwise 
    public bool IsWaiting() => _isWaiting;

    /// Returns true if the cellulo is frozen in place, false otherwise 
    public bool IsFrozen() => _frozen;

    /// Computes the length of the path between the current position and a target 
    public float DistanceBetween(Vector2 current, Vector2 target, bool avoidPlayer)
    {
        float previousDistance = _distanceToTarget;
        Queue<Node> previousNodes = _finalNodes;
        Queue<IPathway> previousPathways = _finalPath;

        float distance = SetTarget(current, target, avoidPlayer);
        _finalNodes = previousNodes;
        _finalPath = previousPathways;
        _distanceToTarget = previousDistance;
        return distance;
    }

    /// Returns the distance to the current target
    public float DistanceToTarget() => _distanceToTarget;
    
    public override string ToString()
    {
        return $"Pathfinder [nodes = {ListToString(_nodes)}]";
    }
    
    [Obsolete("----- For debugging only! -----")]
    private Node FindNode(float x, float y)
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
        List<Node> nodes = Nodes().OrderBy(n => Vector2.Distance(n.Position(), _map.Center())).ToList();

        float radius = Vector2.Distance(nodes[0].Position(), _map.Center());
        string str = $"\nRing with radius {radius} : ";
        int ringID = 0;
        foreach (Node node in nodes)
        {
            float newRadius = Vector2.Distance(node.Position(), _map.Center());
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

    private ISet<Node> Nodes() => _nodes;
    
    public CircularMap Map() => _map;

    private class Edge
    {
        private readonly Node _node1;
        private readonly Node _node2;
        private readonly float _length;
        private readonly float _cost;
        
        // TODO : Get rid of isOccupied
        private readonly bool _isOccupied;
        private readonly IPathway _pathway;

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
    private class Node
    {
        private readonly Vector2 _position;
        private readonly ISet<Edge> _edges;
        private readonly ISet<Node> _neighbors;
        private readonly bool _isBlocking;

        public Node(Vector2 position, bool isBlocking) : this(position, new HashSet<Edge>(), isBlocking) {}
        public Node(Vector2 position, ICollection<Edge> edges, bool isBlocking)
        {
            _position = position;
            _edges = new HashSet<Edge>(edges);
            _neighbors = new HashSet<Node>();
            _isBlocking = isBlocking;
            foreach (Edge edge in _edges)
            {
                foreach (Node node in edge.Nodes())
                {
                    _neighbors.Add(node);
                }
            }
        }
        public bool IsConnectedTo(Node otherNode) => _neighbors.Contains(otherNode);

        private List<Edge> EdgesTo(Node node)
        {
            if (!_neighbors.Contains(node)) return null;
            if (node.Equals(this)) throw new ArgumentException("This node and the target node must be different!");
            
            List<Edge> possibleEdges = _edges.ToList().FindAll(e => e.HasNode(node));
            return possibleEdges;
        }

        public Edge EdgeTo(Node node)
        {
            List<Edge> edges = EdgesTo(node);
            if (IsNull(edges) || edges.Count == 0) return null;
            if (edges.Count > 1) throw new Exception("Duplicate edges. Try using the other method");
            return edges[0];
        }
        
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

        public void SplitConnection(Edge edge, Node newNode, float length, IPathway path, float cost = BASE_COST)
        {
            _neighbors.Remove(edge.OtherNode(this));
            _edges.Remove(edge);
            _edges.Add(new Edge(this, newNode, length, path, cost));
            _neighbors.Add(newNode);
        }

        public void Disconnect(Node node)
        {
            _edges.Remove(EdgeTo(node));
            _neighbors.Remove(node);
            
            if (node.IsConnectedTo(this))
            {
                node.Disconnect(this);
            }
        }

        public bool IsBlocking() => _isBlocking;

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
}

﻿using static Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Diagnostics;
using Vector2 = UnityEngine.Vector2;

public class CircularMap
{
    public const float MARGIN = 1.0f;    // "Wiggle room" to prevent collision between a Cellulo and map borders or other cellulos
    private const float EPSILON = 1e-4f;   // Tolerance regarding floating point values equality
    public const float CHEAT_DETECTION = 0.7f;

    private Vector2 _center;
    private IList<MapRing> _rings = new List<MapRing>();
    private ISet<Passageway> _passages = new HashSet<Passageway>();

    /// Use this to create a custom map, and then use AddRing() and AddPassage()
    public CircularMap(Vector2 center)
    {
        _center = center;
    }
    
    /// Creates a circular map with evenly space rings. ***Not for the current map layout***
    public CircularMap(Vector2 center, int ringsCount)
    {
        if (ringsCount < 1) throw new ArgumentException("There must be at least 1 ring");
        _center = center;
        for (int i = 0; i < ringsCount; ++i)
        {
            _rings.Add(new MapRing(2*(i+1)*MARGIN, center));
        }
    }
    
    [Obsolete("May be deleted later")]
    public CircularMap(Vector2 center, float smallestRadius) : this(center, smallestRadius, new HashSet<Passageway>()) {}

    [Obsolete("May be deleted later")]
    public CircularMap(Vector2 center, float smallestRadius, ICollection<Passageway> passages)
    {
        if (smallestRadius <= 5 * MARGIN)
        {
            throw new ArgumentException("Physical map too small to accomodate circular map");
        }
        float currentRadius = 2 * MARGIN;
        while (currentRadius + MARGIN <= smallestRadius)
        {
            _rings.Add(new MapRing(currentRadius, center));
            currentRadius += 2 * MARGIN;
        }

        _center = _rings[0].Center();

        foreach (Passageway passageway in passages)
        {
            if (passageway != null) _passages.Add(passageway);
        }
    }

    /// Adds a new ring to the map at the desired position (i.e the ring will go through a specified position)
    public MapRing AddRing(Vector2 position)
    {
        return AddRing((position - _center).magnitude);
    }

    /// Adds a new ring to the map with a specified radius
    public MapRing AddRing(float radius)
    {
        MapRing ring = new MapRing(radius, _center);
        _rings.Add(ring);
        _rings = _rings.OrderBy(r => r.Radius()).ToList();
        return ring;
    }

    /// <summary>
    ///     Adds a new passage on the map, with specified direction and smaller ring. Will automatically connect to the closest larger ring
    /// </summary>
    /// <param name="firstRingIndex">The index of the ring from which the passage will span</param>
    /// <param name="orientation">The orientation of the passage</param>
    /// <returns>The added passage</returns>
    public Passageway AddPassage(int firstRingIndex, Vector2 orientation)
    {
        //_passages.Add(Passageway.FromMap(this, firstRingIndex, direction));
        return AddPassage(_center + (MARGIN + 2*MARGIN*(firstRingIndex + 1))*orientation.normalized);
    }
    
    /// <summary>
    ///     Adds a passage that will go through a specified point. Will automatically connect to the closest rings
    /// </summary>
    /// <param name="target">The point which the passage will go through</param>
    /// <returns>The added passage</returns>
    /// <exception cref="ArgumentException">If the point is too close to a ring</exception>
    public Passageway AddPassage(Vector2 target)
    {
        bool isOnSomeRing = false;
        foreach (MapRing ring in _rings)
        {
            isOnSomeRing = isOnSomeRing || ring.IsOn(target);
        }
        if (isOnSomeRing) throw new ArgumentException("Target too ambiguous. Try placing marker further from rings");

        IList<MapRing> borders = ClosestRings(target);
        Passageway passage = new Passageway(borders[0], borders[1], target);
        _passages.Add(passage);
        return passage;
    }
    
    /// <summary>
    ///     Use this to detect if the player is cheating, and then take appropriate actions
    /// </summary>
    /// <param name="position">The position of the player</param>
    /// <returns>True if the player is cheating, false if not</returns>
    public bool IsCheating(Vector2 position)
    {
        return Vector2.Distance(FindClosestPoint(position), position) > CHEAT_DETECTION;
    }

    /// Finds the closest point on the map from a specified target
    public Vector2 FindClosestPoint(Vector2 target)
    {
        Vector2 closest = new Vector2(0,0);
        float distance = float.MaxValue;
        foreach (MapRing ring in _rings)
        {
            float newDistance = ring.DistanceFromPath(target);
            if (newDistance < distance)
            {
                closest = ring.ClosestTo(target);
                distance = newDistance;
            }
        }
        foreach (Passageway passage in _passages)
        {
            float newDistance = passage.DistanceFromPath(target);
            if (newDistance < distance)
            {
                closest = passage.ClosestTo(target);
                distance = newDistance;
            } 
        }

        return closest;
    }
    
    public IPathway FindClosestPathway(Vector2 target)
    {
        Func<IPathway, float> sorter = p => p.DistanceFromPath(target);
        IPathway closestRing = MinElement(_rings, sorter);
        IPathway closestPassage = MinElement(_passages, sorter);
        
        return MinElement(closestRing, closestPassage, sorter);
    }

    public IList<MapRing> ClosestRings(Vector2 position)
    {
        if (_rings.Count <= 1)
        {
            return new List<MapRing> { _rings[0] };
        }  
        if (_rings.Count == 2)
        {
            return _rings[0].Radius() < _rings[1].Radius() ? new List<MapRing> { _rings[0], _rings[1] } : new List<MapRing> { _rings[1], _rings[0] };
        }
        
        List<MapRing> sorted = _rings.OrderBy(r => Math.Abs(r.DistanceFromPath(position))).ToList();
        return new List<MapRing> {sorted[0], sorted[1]};
    }

    public IList<Vector2> PassagesPointsOnRing(MapRing ring)
    {
        ISet<Vector2> list = new HashSet<Vector2>();
        foreach (Passageway p in _passages)
        {
            if (p.SmallRing().Equals(ring)) list.Add(p.SmallPoint());
            if (p.LargeRing().Equals(ring)) list.Add(p.LargePoint());
        }

        return list.ToList();
    }

    public List<MapRing> Rings()
    {
        List<MapRing> list = new List<MapRing>(_rings).OrderBy(ring => ring.Radius()).ToList();
        return list;
    }

    public ISet<Passageway> Passages() => new HashSet<Passageway>(_passages);

    public List<IPathway> Pathways()
    {
        List<IPathway> pathways = new List<IPathway>(_rings);
        pathways.AddRange(_passages);
        return pathways;
    }

    public Vector2 Center() => _center;

    public override string ToString()
    {
        return $"CircularMap [rings = {ListToString(_rings)}, \npassages = {ListToString(_passages)}";
    }
    
    public interface IPathway
    {
        public Vector2 ClosestTo(Vector2 target);

        public float DistanceFromPath(Vector2 target);

        public float DistanceBetween(Vector2 pointA, Vector2 pointB, bool forceDetour = false);

        public Vector2 Orientate(Vector2 position, Vector2 target);

        public bool IsOn(Vector2 point);

        // TODO: Uncomment this when Unity is updated to newest version
        /*
        public bool IsCloseEnoughTo(Vector2 target, float minDistance)
        {
            return DistanceTo(target) < minDistance;
        } 
        */
    }
    
    public class Passageway : IPathway
    {
        private readonly MapRing _smallRing;
        private readonly MapRing _largeRing;
        private readonly Vector2 _smallPoint;
        private readonly Vector2 _largePoint;
        public static Passageway FromMap(CircularMap map, int firstRingIndex, Vector2 direction)
        {
            if (map._rings.Count <= 1)
            {
                //throw new ArgumentException("Not enough rings to create passageway");
            }
            if (!(0 <= firstRingIndex && firstRingIndex < map.Rings().Count)) throw new ArgumentException($"Index must be between 0 and {map.Rings().Count} (number of rings - 1)");
            return new Passageway(map.Rings()[firstRingIndex], map.Rings()[firstRingIndex + 1], map._center + direction);
        }
        
        /// The passageway is assumed to radiate from the center of the rings
        public Passageway(MapRing ring1, MapRing ring2, Vector2 position)
        {
            //TODO : Change to direction instead of position
            if (ring1 == null || ring2 == null) throw new ArgumentException("Rings must NOT be null!");
            if (ring1.Equals(ring2)) throw new ArgumentException("Rings must be different from each other!");
            if (Mathf.Abs(Vector2.Distance(position, ring1.Center()) - ring1.Radius()) < EPSILON)
            {
                throw new ArgumentException("Position too close to the rings. Try to find another position");
            }
            
            if (ring1.Radius() < ring2.Radius()) 
            {
                _smallRing = ring1;
                _largeRing = ring2;
            }
            else
            {
                _smallRing = ring2;
                _largeRing = ring1;
            }

            float angle = Vector2.SignedAngle(Vector2.right, position - _smallRing.Center());
            _smallPoint = _smallRing.PointAt(angle);
            _largePoint = _largeRing.PointAt(angle);
        }

        public ISet<MapRing> Rings() => new HashSet<MapRing> { _smallRing, _largeRing };

        public ISet<Vector2> Points() => new HashSet<Vector2> { _smallPoint, _largePoint };

        public MapRing SmallRing() => _smallRing;
        
        public MapRing LargeRing() => _largeRing;
        
        public Vector2 SmallPoint() => _smallPoint;
        
        public Vector2 LargePoint() => _largePoint;
        

        public float Length() => Vector2.Distance(_smallPoint, _largePoint);

        public Vector2 ClosestTo(Vector2 target)
        {
            //return _smallPoint + Projection(target, _largePoint - _smallPoint);
            Vector2 projected = _smallPoint + Projection(target - _smallPoint, _largePoint - _smallPoint);
            if (!IsOn(projected))
            {
                float distance1 = Vector2.Distance(projected, _smallPoint);
                float distance2 = Vector2.Distance(projected, _largePoint);
                return distance1 < distance2 ? _smallPoint : _largePoint;
            }

            return projected;

        }
        
        public float DistanceBetween(Vector2 pointA, Vector2 pointB, bool forceDetour = false)
        {
            return Vector2.Distance(ClosestTo(pointA), ClosestTo(pointB));
        }

        public float DistanceFromPath(Vector2 target)
        {
            /*
            if (!IsOn(target))
            {
                return Mathf.Min(Vector2.Distance(target, _smallPoint), Vector2.Distance(target, _largePoint));
            }
            return Vector2.Distance(ClosestTo(target), target);
            */
            Vector2 projected = _smallPoint + Projection(target - _smallPoint, _largePoint - _smallPoint);
            
            if (IsOn(projected))
            {
                return Vector2.Distance(projected, target);
            }
            return Mathf.Min(Vector2.Distance(target, _smallPoint), Vector2.Distance(target, _largePoint)); 
            
            //return Vector2.Distance(projected, target);

        }

        public bool IsOn(Vector2 position)
        {
            return Mathf.Abs(Vector2.Distance(position, _smallPoint) + Vector2.Distance(position, _largePoint) 
                            - Length()) < EPSILON;
        }

        public Vector2 Orientate(Vector2 position, Vector2 target)
        {
            //TODO : Implement this
            Vector2 orientation = (_largePoint - _smallPoint).normalized;
            return Vector2.Dot(target - position, orientation) > 0 ? orientation : -orientation;
            //return (target - position).normalized;
        }

        /*
        public Vector2 Direction(Vector2 currentPos, Vector2 nextPos)
        {
            if (!IsOn(currentPos)) throw new ArgumentException("Current position must be on the passageway!");
            return (nextPos - currentPos).normalized;
        }
        **/

        private Vector2 Projection(Vector2 vector, Vector2 axis)
        {
            //return Vector2.Dot(axis.normalized, vector)*axis;
            return Vector2.Dot(axis.normalized, vector)*axis.normalized;
            //return axis * (float)(vector.magnitude * Math.Cos(Vector2.Angle(axis, vector)));
        }
        
        public override bool Equals(object obj)
        {
        
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (Passageway)obj;

            return _smallRing.Equals(other.SmallRing()) && _largeRing.Equals(other._largeRing) &&
                   AreSame(_smallPoint, other._smallPoint) && AreSame(_largePoint, other._largePoint);
        }

        public override string ToString()
        {
            return $"Passageway [From {_smallPoint}, to {_largePoint}]";
        }
        
    }

    public class MapRing : IPathway
    {
        private readonly float _radius;
        private readonly Vector2 _center;

        public MapRing(CircularMap map, int ringIndex)
        {
            if (!(0 <= ringIndex && ringIndex < map.Rings().Count)) throw new ArgumentException($"Index must be between 0 and {map.Rings().Count} (number of rings - 1)");
            _radius = map.Rings().OrderBy(r => r._radius).ToList()[ringIndex]._radius;
      
            if (_radius <= 0) throw new ArgumentException($"Negative radius. There is something wrong with {map.Rings()[ringIndex]}");
            _center = map.Rings()[0]._center;
        }
        
        //[Obsolete("Works only with a map with uniformly spaced rings, i.e linear radii")]
        public MapRing(float radius, Vector2 center)
        {
            if (radius <= 0) throw new ArgumentException("Radius can't be null or negative!");
            _radius = radius;
            _center = center;
        }

        public float DistanceFromCenter(Vector2 point)
        {
            return (_center - point).magnitude;
        }

        public float DistanceBetween(Vector2 from, Vector2 to, bool forceDetour = false)
        {
            float angle = Vector2.Angle(from - _center, to - _center);
            angle = forceDetour ? 360 - angle : angle;
            return Mathf.Abs(Mathf.PI * angle * _radius / 180f);
            /*
            bool isClockwise = !forceDetour || Vector2.Angle(pointA - _center, pointB - _center) < 90;
            float angle = Mathf.Abs(Vector2.Angle(pointA - _center, pointB - _center));
            angle = isClockwise ? angle : 360 - angle;
            return (float)Mathf.Abs(Mathf.PI * angle * _radius / 180f);
            */
        }

        public bool IsOn(Vector2 point)
        {
            return Mathf.Abs(DistanceFromCenter(point) - _radius) <= EPSILON;
        }

        /// <summary>
        ///     Finds the point on the ring that has a given angle from the horizontal line
        /// </summary>
        /// <param name="angle">Angle from the horizontal line (trignonmetric direction, i.e. counter-clockwise)</param>
        /// <returns>A point on the ring that has a given angle from the horizontal line</returns>
        public Vector2 PointAt(float angle)
        {
            return new Vector2((float)(_center.x + _radius * Mathf.Cos(ToRadians(angle))), (float)(_center.y + _radius * Mathf.Sin(ToRadians(angle))));
        }
        
        public float Radius() => _radius;

        public Vector2 Center() => _center;

        public Vector2 Orientate(Vector2 position, Vector2 target)
        {
            // TODO : Account for blocked edges
            bool isClockwise = Vector2.SignedAngle(position - _center, target - _center) < 0;
            
            return Direction(position, isClockwise);
            //bool isClockwise = !forceDetour || Vector2.Angle(position - _center, target - _center) < 180;
            //return Direction(position, isClockwise);
            //bool isClockwise = Vector2.Angle(currentDirection, Direction(position, true)) < 90;
            //bool isClockwise = Vector2.SignedAngle(currentDirection, Direction(position, true)) < 0;
            //return Direction(position, isClockwise);
        }


        public Vector2 Direction(Vector2 currentPos, bool isClockWise)
        {
            if (!IsOn(currentPos))
            {
                //throw new ArgumentException("Current position must be on ring!");
            }

            //Vector3 w = new Vector3(0, 1,0);
            //Vector3 r = ToVector3(currentPos - _center, 0);
            //w = w * (10/r.magnitude);
            //Vector3 direction = Vector3.Cross(w, r);
            /*
            Vector3 w = new Vector3(0, 0, 1);
            Vector3 r = new Vector3((currentPos - _center).x, (currentPos - _center).y, 0).normalized * _radius;
            w = w * 2;
            Vector3 cross = Vector3.Cross(w, r);
            Vector3 direction3 = new Vector3(cross.x, 0, cross.y);
            Vector2 direction = ToVector2(direction3);   
            */   
            
            Vector2 direction = Vector2.Perpendicular(currentPos - _center).normalized;
            
            return isClockWise ? -direction: direction;
        }

        public Vector2 ClosestTo(Vector2 target)
        {
            return PointAt(Vector2.Angle(Vector2.right, target - _center));
        }

        public float DistanceFromPath(Vector2 target)
        {
            return Mathf.Abs(DistanceFromCenter(target) - _radius);
        }
        
        public override bool Equals(object obj)
        {
        
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MapRing)obj;

            return AreSame(_center, other._center) && AreSame(_radius, other._radius);
        }

        public override string ToString()
        {
            return $"MapRing [center = {_center}, radius = {_radius,1:F2}]";
        }
        
    }
    
}




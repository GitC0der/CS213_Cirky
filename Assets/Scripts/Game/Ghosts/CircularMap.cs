using static Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Diagnostics;

public class CircularMap
{
    private const float MARGIN = 1.0f;    // "Wiggle room" to prevent collision between a Cellulo and map borders or other cellulos
    private const float EPSILON = 1e-4f;   // Tolerance regarding floating point values equality

    private Vector2 _center;
    private IList<MapRing> _rings = new List<MapRing>();
    private ISet<Passageway> _passages = new HashSet<Passageway>();

    public CircularMap(Vector2 center, float smallestDiameter, ICollection<Passageway> passages, ICollection<CelluloAgent> cellulos)
    {
        if (smallestDiameter <= 5 * MARGIN)
        {
            throw new ArgumentException("Physical map too small to accomodate circular map");
        }
        float currentRadius = 2 * MARGIN;
        while (currentRadius + MARGIN <= smallestDiameter)
        {
            _rings.Add(new MapRing(currentRadius, center));
            currentRadius += 2 * MARGIN;
        }

        _center = _rings[0].Position();

        foreach (Passageway passageway in passages)
        {
            if (passageway != null) _passages.Add(passageway);
        }
    }

    public void AddNewPassage(int firstRingIndex, Vector2 target)
    {
        _passages.Add(Passageway.FromMap(this, firstRingIndex, target));
    }
    public void AddNewPassage(Vector2 target)
    {
        bool isOnSomeRing = false;
        foreach (MapRing ring in _rings)
        {
            isOnSomeRing = isOnSomeRing || ring.IsOn(target);
        }
        if (isOnSomeRing) throw new ArgumentException("Target too ambiguous. Try placing marker further from rings");

        IList<MapRing> borders = ClosestRings(target);
        _passages.Add(new Passageway(borders[0], borders[1], target));
    }

    public IList<MapRing> ClosestRings(Vector2 position)
    {
        if (_rings.Count <= 1)
        {
            return new List<MapRing> { _rings[0] };
        }  
        if (_rings.Count == 2)
        {
            return new List<MapRing> { _rings[0], _rings[1] };
        }
        MapRing first = _rings[0];
        MapRing second = _rings[1];
        
        foreach (MapRing ring in _rings)
        {
            if (ring.DistanceFromCenter(position) < first.DistanceFromCenter(position))
            {
                second = first;
                first = ring;
            }
            else if (ring.DistanceFromCenter(position) < second.DistanceFromCenter(position))
            {
                second = ring;
            }
        }

        return new List<MapRing> { first, second };
    }

    public IList<Vector2> PassagesOnRing(MapRing ring)
    {
        IList<Vector2> list = new List<Vector2>();
        foreach (Passageway p in _passages)
        {
            if (p.SmallRing().Equals(ring)) list.Add(p.SmallPoint());
            if (p.LargeRing().Equals(ring)) list.Add(p.LargePoint());
        }

        return list;
        /*
        IList<Passageway> list = new List<Passageway>(_passages);
        return list.TakeWhile(p => p.SmallRing().Equals(ring) || p.LargeRing().Equals(ring)) as IList<Passageway>;
        */
    }

    public List<MapRing> Rings()
    {
        List<MapRing> list = new List<MapRing>(_rings).OrderBy(ring => ring.Radius()).ToList();
        //List<MapRing> list = new List<MapRing>(_rings);
        //list.Sort((r1,r2) => r1.Radius().CompareTo(r2.Radius()));
        return list;
    }

    public ISet<Passageway> Passages() => new HashSet<Passageway>(_passages);

    public Vector2 Center() => _center;

    public override string ToString()
    {
        return $"CircularMap[rings = {ListToString(_rings)}, \npassages = {ListToString(_passages)}";
    }
    
    public class Passageway
    {
        private readonly MapRing _smallRing;
        private readonly MapRing _largeRing;
        private readonly Vector2 _smallPoint;
        private readonly Vector2 _largePoint;
        public static Passageway FromMap(CircularMap map, int firstRingIndex, Vector2 position)
        {
            if (map._rings.Count <= 1)
            {
                //throw new ArgumentException("Not enough rings to create passageway");
            }
            if (!(0 <= firstRingIndex && firstRingIndex < map.Rings().Count)) throw new ArgumentException($"Index must be between 0 and {map.Rings().Count} (number of rings - 1)");
            return new Passageway(map.Rings()[firstRingIndex], map.Rings()[firstRingIndex + 1], position);
        }
        
        /** The passageway is assumed to radiate from the center of the rings */
        public Passageway(MapRing ring1, MapRing ring2, Vector2 position)
        {
            if (ring1 == null || ring2 == null) throw new ArgumentException("Rings must NOT be null!");
            if (ring1.Equals(ring2)) throw new ArgumentException("Rings must be different from each other!");
            if (Math.Abs(Vector2.Distance(position, ring1.Position())) < EPSILON)
            {
                throw new ArgumentException("Position too close to ring centers. Try to find another position");
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

            float angle = Vector2.Angle(Vector2.right, position - _smallRing.Position());
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

        public bool IsOn(Vector2 position)
        {
            return Math.Abs(Vector2.Distance(position, _smallPoint) + Vector2.Distance(position, _largePoint) 
                            - Length()) < EPSILON;
        }
        
        public Vector2 Direction(Vector2 currentPos, Vector2 nextPos)
        {
            if (!IsOn(currentPos)) throw new ArgumentException("Current position must be on the passageway!");
            return (nextPos - currentPos).normalized;
        }

        private Vector2 Projection(Vector2 vector, Vector2 axis)
        {
            return Vector2.Dot(axis.normalized, vector)*axis;
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
            return $"Passageway[point1 = {_smallPoint}, point2 = {_largePoint}]";
        }
    }

    public class MapRing
    {
        private readonly float _radius;
        private readonly Vector2 _center;

        public MapRing(CircularMap map, int ringIndex)
        {
            if (!(0 <= ringIndex && ringIndex < map.Rings().Count)) throw new ArgumentException($"Index must be between 0 and {map.Rings().Count} (number of rings - 1)");
            _radius = map.Rings()[ringIndex]._radius;
            if (_radius <= 0) throw new ArgumentException($"Negative radius. There is something wrong with {map.Rings()[ringIndex]}");
            _center = map.Rings()[0]._center;
        }
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

        public float DistanceBetween(Vector2 pos1, Vector2 pos2)
        {
            return _radius * Math.Abs(Vector2.Angle(pos1 - _center, pos2 - _center));
        }

        public bool IsOn(Vector2 point)
        {
            return Math.Abs(DistanceFromCenter(point) - _radius) <= EPSILON;
        }

        /// <summary>
        ///     Finds the point on the ring that has a given angle from the horizontal line
        /// </summary>
        /// <param name="angle">Angle from the horizontal line (trignonmetric direction, i.e. counter-clockwise)</param>
        /// <returns>A point on the ring that has a given angle from the horizontal line</returns>
        public Vector2 PointAt(float angle)
        {
            return new Vector2((float)(_radius * Math.Cos(angle)), (float)(_radius * Math.Sin(angle)));
        }

        public float Radius() => _radius;

        public Vector2 Position() => _center;

        public Vector2 Direction(Vector2 currentPos, bool isClockWise)
        {
            if (!IsOn(currentPos))
            {
                //throw new ArgumentException("Current position must be on ring!");
            }
            //Vector2 direction = PointAt(Vector2.Perpendicular((currentPos - _center).normalized));
            Vector2 direction = PointAt(Vector2.Angle(Vector2.right, Vector2.Perpendicular((currentPos - _center).normalized)));
            return isClockWise ? direction: -direction;
        }
        
        public override bool Equals(object obj)
        {
        
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MapRing)obj;

            return AreSame(_center, other._center) && AreSame(_radius, other._radius);
        }

        public override string ToString()
        {
            return $"MapRing[center = {_center}, radius = {_radius,1:F2}]";
        }
        
    }
    
}




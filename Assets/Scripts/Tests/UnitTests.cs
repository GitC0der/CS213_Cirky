using static Utils;
using static CircularMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 *  The Unity Test Framework was way too hard to set up so this ugly solution is used.
 */
public class UnitTests : MonoBehaviour
{
    private void LaunchTests()
    {
        //MapRingTests();
        //PassagesOnRingTest();
        FindExistingNode_Works();
    }

    private void FindExistingNode_Works()
    {
        var direction1 = new Vector2(1, 4);
        var direction2 = new Vector2(-1, -1);
        CircularMap map = new CircularMap(new Vector2(3, 3), 9.1f, new List<Passageway>(), new List<CelluloAgent>());
        map.AddNewPassage(0, direction1);
        map.AddNewPassage(0, direction2);
        map.AddNewPassage(1, direction1);
        map.AddNewPassage(2, direction2);
        map.AddNewPassage(2, direction1);
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
        CircularMap map = new CircularMap(center, 12, new List<CircularMap.Passageway> {passage1, passage2, falsePassage}, new List<CelluloAgent>());
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

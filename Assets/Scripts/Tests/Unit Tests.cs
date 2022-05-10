using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 *  The Unity Test Framework was way too hard to set up so this ugly solution is used.
 */
public class UnitTests : MonoBehaviour
{
    private bool testsDone = false;
    
    private void LaunchTests()
    {
        MapRingTests();
    }

    private void MapRingTests()
    {
        CircularMap.MapRing ring = new CircularMap.MapRing(5, new Vector2(3,9));
        Debug.Log(ring);
    }

    // Update is called once per frame
    void Update()
    {
        if (!testsDone) LaunchTests();
        testsDone = true;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
}

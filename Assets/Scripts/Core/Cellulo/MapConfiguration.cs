using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> 
/// The main cellulo manager 
/// </summary> 
[Serializable]
public class MapConfiguration : MonoBehaviour
{
    public PaperSize paper;
    public int REAL_MAP_DIMENSION_X; 
    public int REAL_MAP_DIMENSION_Y;  

    public GameObject map;

    public void Awake(){
        UpdateParameters();
    }
    public void UpdateParameters(){
        UpdatePaperSize();
        UpdateCelluloUnityScale();
    }
    public void UpdatePaperSize(){
        switch(paper){
            case PaperSize.A0: 
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 841; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 1189; 
                break; 
            case PaperSize.A1: 
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 594 ; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 841; 
                break;
            case PaperSize.A2: 
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 420 ; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 594; 
                break;
            case PaperSize.A3: 
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 297 ; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 420; 
                break;
            case PaperSize.A4: 
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 210 ; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 297; 
                break;
            case PaperSize.Custom: 
                Config.REAL_MAP_DIMENSION_X  = REAL_MAP_DIMENSION_X;
                Config.REAL_MAP_DIMENSION_Y  = REAL_MAP_DIMENSION_Y;
                break;
            default:  
                REAL_MAP_DIMENSION_X = Config.REAL_MAP_DIMENSION_X = 297 ; 
                REAL_MAP_DIMENSION_Y = Config.REAL_MAP_DIMENSION_Y = 420;  
                break;
        }
    }    

    public void UpdateCelluloUnityScale(){
        CelluloAgent [] agents = Resources.FindObjectsOfTypeAll<CelluloAgent>();
        foreach( CelluloAgent agent in agents){
            agent.SetRobotScale();
        }
        map.transform.localPosition = -new Vector3(0,(Mathf.Ceil(10*Config.GetCelluloScale()*0.448f/2))/10.0f,0);
    }
}

public enum PaperSize{
    A0, A1, A2, A3, A4, Custom
}

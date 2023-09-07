using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerPointCreator : MonoBehaviour
{
    public Transform sphere;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    bool init = false;

    // Update is called once per frame
    void Update()
    {
        // For initialization
        if (!init && transform.childCount > 0)
        {
            sphere.transform.parent = transform.Find("Bones").Find("Hand_WristRoot").Find("Hand_Index1").Find("Hand_Index2").Find("Hand_Index3").Find("Hand_IndexTip");
            sphere.localPosition = Vector3.zero;

            init = true;
        }
    }
}

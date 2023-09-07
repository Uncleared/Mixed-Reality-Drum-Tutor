using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class KeyboardInstantiator : MonoBehaviour
{
    public OVRHand rightHand;
    public OVRHand leftHand;

    public Vector3 offset;
    public Transform point1;
    public Transform point2;

    public Transform keyboard;
    public MeshRenderer keyboardRenderer;
    // Start is called before the first frame update
    void Start()
    {
        keyboardRenderer.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            keyboard.position = (point1.position + point2.position) / 2f + offset;
            keyboard.rotation = Quaternion.FromToRotation(Vector3.right, (point2.position - point1.position).normalized);
            //keyboard.rotation = Quaternion.rot
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            //keyboardRenderer.enabled = true;
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            //keyboardRenderer.enabled = false;
        }
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) || (leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index)))
        {
            keyboard.position = (point1.position + point2.position) / 2f + offset;
            keyboard.rotation = Quaternion.FromToRotation(Vector3.right, (point2.position - point1.position).normalized);
        }
    }
}

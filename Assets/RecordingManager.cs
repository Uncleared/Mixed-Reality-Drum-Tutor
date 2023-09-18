using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecordingManager : MonoBehaviour
{
    public TextMeshProUGUI selectedSlotText;
    public static int recordingSlot = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        selectedSlotText.text = "Slot: " + (recordingSlot + 1);
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            recordingSlot = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            recordingSlot = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            recordingSlot = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            recordingSlot = 3;
        } 
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            recordingSlot = 4;
        }
    }
}

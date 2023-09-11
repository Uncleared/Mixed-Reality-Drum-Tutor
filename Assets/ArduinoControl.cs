using UnityEngine;
using System.Collections;
using System.IO.Ports;

public class ArduinoControl : MonoBehaviour
{
    public string portName;
    SerialPort arduino;

    public string stringToSend = "10001";

    void Start()
    {
        arduino = new SerialPort(portName, 9600);
        arduino.Open();
    }

    void Update()
    {
        if (arduino.IsOpen)
        {
            if (Input.GetKeyDown("1"))
            {
                arduino.Write(stringToSend + "\r\n");
                Debug.Log(1);
            }
            //else if (Input.GetKey("0"))
            //{
            //    arduino.Write("10101");
            //    Debug.Log(0);
            //}
        }
    }

    private void OnApplicationQuit()
    {
        print("Closing");
        arduino.Close();
    }
}
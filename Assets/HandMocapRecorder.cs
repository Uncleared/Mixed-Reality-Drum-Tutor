using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public enum HandSide
{
    LeftHand,
    RightHand,
}

public class HandMocapRecorder : MonoBehaviour
{
    public Transform relativePoint;

    public HandSide handSide;
    public Dictionary<string, Transform> boneDictionary;
    public Dictionary<string, Vector3> targetBoneRotations;
    public List<Transform> bones;
    public List<string> boneNames;
    public Transform realRoot;
    public Transform root;

    string decidedFilePath;

    private void Start()
    {
        boneDictionary = new Dictionary<string, Transform>();
        bones = new List<Transform>();
        boneNames = new List<string>();
    }

    void AddChildren(Transform trans)
    {
        if (trans.gameObject.activeInHierarchy)
        {
            boneDictionary[trans.name] = trans;
            bones.Add(trans);
            boneNames.Add(trans.name);
            foreach (Transform child in trans)
            {

                //child.transform.localEulerAngles = Random.insideUnitSphere;
                if (child.childCount > 0)
                {
                    AddChildren(child);
                }
            }
        }

    }
    public object[] RecordFrame(List<Transform> bones, float currentTime)
    {
        int boneCount = boneNames.Count;
        object[] streamedData = new object[boneCount * 2 + 4];

        // store the bone count
        streamedData[0] = boneCount;
        for (int i = 0; i < boneCount; i++)
        {
            streamedData[i + 1] = boneNames[i];
            //streamedData[i + 1] = Random.insideUnitSphere * 90f;
        }

        for (int i = 0; i < boneCount; i++)
        {
            streamedData[boneNames.Count + i + 1] = bones[i].eulerAngles;
            //streamedData[i + 1] = Random.insideUnitSphere * 90f;
        }

        if(relativePoint != null)
        {
            streamedData[boneCount * 2 + 1] = realRoot.transform.eulerAngles - relativePoint.eulerAngles;
            streamedData[boneCount * 2 + 2] = realRoot.transform.position - relativePoint.position;
        }
        else
        {
            streamedData[boneCount * 2 + 1] = realRoot.transform.eulerAngles;
            streamedData[boneCount * 2 + 2] = realRoot.transform.position;
        }
        streamedData[boneCount * 2 + 3] = currentTime;

        return streamedData;
    }

    string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, RecordingManager.recordingSlot + handSide.ToString() + "handData.csv");
    }


    void SaveFrameToCSV(object[] frame)
    {
        string filePath = decidedFilePath;
        print(filePath);

        // Create a new StreamWriter to write to the CSV file
        StreamWriter streamWriter = new StreamWriter(filePath, true);

        string line = "";
        // Loop through the data and write each row to the CSV file
        foreach (object data in frame)
        {
            line += data.ToString() + "/";
        }

        streamWriter.WriteLine(line);
        // Close the StreamWriter to save the file
        streamWriter.Close();
    }

    bool init = false;

    float currentTime = 0f;
    bool recording = false;

    public void Record()
    {
        if(!init)
        {
            return;
        }
        currentTime = 0f;
        recording = true;
        decidedFilePath = GetFilePath();
        if (File.Exists(decidedFilePath))
        {
            // Attempt to delete the file
            File.Delete(decidedFilePath);
        }
    }

    public void StopRecord()
    {
        if(!init)
        {
            return;
        }
        recording = false;
    }

    private void Update()
    {
        // For initialization
        if (!init && transform.childCount > 0)
        {
            root = transform.Find("Bones").Find("Hand_WristRoot");

            AddChildren(root);
            init = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            recording = !recording;
          
        }
        if(recording)
        {
            currentTime += Time.deltaTime;
            object[] currentFrame = RecordFrame(bones, currentTime);
            SaveFrameToCSV(currentFrame);
        }
    }
}

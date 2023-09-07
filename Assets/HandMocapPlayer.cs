using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class HandMocapPlayer : MonoBehaviour
{
    public Transform relativePoint;
    public HandSide handSide;


    public Dictionary<string, Transform> boneDictionary;
    public Dictionary<string, Vector3> targetBoneRotations;
    public List<Transform> bones;
    public List<string> boneNames;
    public Transform root;

    Vector3 targetRootRotation;
    Vector3 targetRootTranslation;

    public Transform realRoot;
    public float slerpSpeed = 5f;

    
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

    Vector3 StringToVector3(string stringVector)
    {
        string processed = Regex.Replace(stringVector, "[() ]", "");
        string[] split = processed.Split(',');
        return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
    }
    public void AssignBoneData(string[] bonesData)
    {
        targetBoneRotations = new Dictionary<string, Vector3>();

        List<Vector3> boneEulerAngles = new List<Vector3>();
        List<string> boneNames = new List<string>();

        Vector3 realRootRotation = Vector3.zero;
        Vector3 realRootTranslation = Vector3.zero;

        int boneCount = int.Parse(bonesData[0]);

        // Get the names from the stream
        for (int i = 0; i < boneCount + 1; i++)
        {
            if (i != 0)
            {
                boneNames.Add((string)bonesData[i]);
            }
        }

        // Get the bone euler angles from the stream
        for (int i = boneCount + 1; i < bonesData.Length - 2; i++)
        {
            if (i != 0)
            {
                boneEulerAngles.Add(StringToVector3(bonesData[i]));
            }
        }

        // Apply the bone rotations
        for (int i = 0; i < boneNames.Count; i++)
        {
            if (boneDictionary.ContainsKey(boneNames[i]))
            {
                boneDictionary[boneNames[i]].eulerAngles = boneEulerAngles[i];
                //targetRotations[boneNames[i]] = boneEulerAngles[i];
            }
        }

        // Apply the root rotation and translation
        realRootTranslation = StringToVector3(bonesData[boneCount * 2 + 2]);
        realRootRotation = StringToVector3(bonesData[boneCount * 2 + 1]);
        //realRoot.transform.eulerAngles = realRootRotation;

        targetRootTranslation = realRootTranslation;
        targetRootRotation = realRootRotation;

        print("Received");
    }


    // Start is called before the first frame update
    void Start()
    {
        boneDictionary = new Dictionary<string, Transform>();
        targetBoneRotations = new Dictionary<string, Vector3>();

        bones = new List<Transform>();
        boneNames = new List<string>();
        AddChildren(root);
    }

    int currentFrame = 0;
    float currentTime = 0f;
    bool playing = false;

    List<string[]> playbackData;

    string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, RecordingManager.recordingSlot + handSide.ToString() + "handData.csv");
    }
    public void Play()
    {
        playbackData = new List<string[]>();

        // Read the data from the csv file and try to assign it
        string filePath = GetFilePath();
        if(File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            // Loop through each line and split it into an array of values
            foreach (string line in lines)
            {
                string[] values = line.Split('/');
                playbackData.Add(values);
                //object[] bonesData = new object[values.Length];
                //for(int i = 0; i < values.Length; i++)
                //{
                //    bonesData[i] = values[i];
                //}
                // Add the array of values to the list
            }
            playing = true;
            currentFrame = 0;
            currentTime = 0f;
        }
       

    }

    public void StopPlay()
    {
        playing = false;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            playing = !playing;
           if(playing)
            {
                Play();
            }
        }

        if(playing)
        {
            currentTime += Time.deltaTime;
            int boneCount = int.Parse(playbackData[currentFrame][0]);
            float neededTime = float.Parse(playbackData[currentFrame][boneCount * 2 + 3]);
            if(currentTime >= neededTime)
            {
                AssignBoneData(playbackData[currentFrame]);
                currentFrame++;
                if (currentFrame == playbackData.Count - 1)
                {
                    playing = false;
                    gameObject.SetActive(false);
                }
            }

      
        }
        foreach (string key in targetBoneRotations.Keys)
        {
            Transform bone = boneDictionary[key].transform;
            bone.eulerAngles = Quaternion.Slerp(bone.rotation, Quaternion.Euler(targetBoneRotations[key]), Time.deltaTime * slerpSpeed).eulerAngles;
        }
        //realRoot.transform.eulerAngles = Quaternion.Slerp(realRoot.rotation, Quaternion.Euler(targetRootRotation), Time.deltaTime * slerpSpeed).eulerAngles;
        //realRoot.transform.position = targetRootTranslation;


        realRoot.transform.eulerAngles = Quaternion.Slerp(realRoot.rotation, Quaternion.Euler(relativePoint.eulerAngles + targetRootRotation), Time.deltaTime * slerpSpeed).eulerAngles;
        realRoot.transform.position = Vector3.Slerp(realRoot.transform.position, relativePoint.position + targetRootTranslation, Time.deltaTime * slerpSpeed);
    }
}

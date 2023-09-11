using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO.Ports;

public class MidiRecorder : MonoBehaviour
{
    public List<HandMocapRecorder> handRecorders;
    public List<HandMocapPlayer> handPlayers;

    public class PressData
    {
        public float timeStamp = 0f;
        public float velocity;
        public int midiNumber;
        public int finger;
    }

    public Image statusImage;
    public Sprite recordingSprite;
    public Sprite playbackSprite;

    public GameObject hitParticlePrefab;

    public List<MeshRenderer> noteHighlights;
    public bool isRecording = false;
    public bool isPlaying = false;

    // on press should contain several properties

    public List<PressData> onPressTimestamps;
    public List<PressData> onReleaseTimestamps;

    //int frame = 0;
    int pressNoteIndex = 0;
    int releaseNoteIndex = 0;

    float currentTime = 0f;

    public List<AudioClip> audioClips;

    public AudioMixerGroup audioMixer;

    SerialPort arduino;


    string GetKeyOnFilePath()
    {
        return Path.Combine(Application.persistentDataPath, RecordingManager.recordingSlot + "keyOnMidiData.csv");
    }

    string GetKeyOffFilePath()
    {
        return Path.Combine(Application.persistentDataPath, RecordingManager.recordingSlot + "keyOffMidiData.csv");
    }

    public void BeginRecord()
    {
        ResetRecording();
        handRecorders.ForEach(x => x.Record());
        currentTime = 0f;
        statusImage.enabled = true;
        statusImage.sprite = recordingSprite;
    }

    public void EndRecord()
    {
        statusImage.enabled = false;
        handRecorders.ForEach(x => x.StopRecord());

        // Save the onPress and onRelease time stamps to a csv
        // Create a new StreamWriter to write to the CSV file
        StreamWriter streamWriter = new StreamWriter(GetKeyOnFilePath(), false);

        // Loop through the data and write each row to the CSV file
        foreach (PressData data in onPressTimestamps)
        {
            streamWriter.WriteLine(data.timeStamp + "," + data.velocity + "," + data.midiNumber + "," + data.finger);
        }

        // Close the StreamWriter to save the file
        streamWriter.Close();



        // Create a new StreamWriter to write to the CSV file
        streamWriter = new StreamWriter(GetKeyOffFilePath(), false);

        // Loop through the data and write each row to the CSV file
        foreach (PressData data in onReleaseTimestamps)
        {
            streamWriter.WriteLine(data.timeStamp + "," + data.velocity + "," + data.midiNumber + "," + data.finger);
        }

        // Close the StreamWriter to save the file
        streamWriter.Close();
    }

    void PlaybackStopped()
    {
        statusImage.enabled = false;
    }
    public void PlayRecorded()
    {
        // Load the data from the file

        onPressTimestamps = new List<PressData>();
        string[] lines = File.ReadAllLines(GetKeyOnFilePath());

        // Loop through each line and split it into an array of values
        foreach (string line in lines)
        {
            string[] values = line.Split(',');
            onPressTimestamps.Add(new PressData { timeStamp = float.Parse(values[0]), velocity = float.Parse(values[1]), midiNumber = int.Parse(values[2]), finger = int.Parse(values[3]) });
        }

        // Load from the file
        onReleaseTimestamps = new List<PressData>();
        lines = File.ReadAllLines(GetKeyOffFilePath());

        // Loop through each line and split it into an array of values
        foreach (string line in lines)
        {
            string[] values = line.Split(',');
            onReleaseTimestamps.Add(new PressData { timeStamp = float.Parse(values[0]), velocity = float.Parse(values[1]), midiNumber = int.Parse(values[2]), finger = int.Parse(values[3]) });
        }
        isPlaying = true;
        currentTime = 0f;
        pressNoteIndex = 0;
        releaseNoteIndex = 0;
        statusImage.enabled = true;
        statusImage.sprite = playbackSprite;
        handPlayers.ForEach(x => {
            x.gameObject.SetActive(true);
            x.Play();
        });

    }

    public void ResetRecording()
    {

        onPressTimestamps = new List<PressData>();
        onReleaseTimestamps = new List<PressData>();
    }

    void DisplayNote(int mappedMidiNumber, float velocity)
    {
        MeshRenderer noteRenderer = noteHighlights[mappedMidiNumber];
        noteRenderer.enabled = true;
        Color velocityColor = Color.Lerp(Color.blue, Color.yellow, velocity);
        noteRenderer.material.color = velocityColor;

        float factor = Mathf.Pow(2, 1f);
        noteRenderer.material.SetColor("_EmissionColor", velocityColor * factor);

        GameObject hitParticle = Instantiate(hitParticlePrefab);
        hitParticle.transform.position = noteRenderer.transform.position;
        hitParticle.SetActive(true);
        ParticleSystem ps = hitParticle.GetComponent<ParticleSystem>();
        ParticleSystem.EmissionModule em = ps.emission;
        ps.startColor = velocityColor;
        ps.GetComponent<ParticleSystemRenderer>().material.EnableKeyword("_EmissionColor");
        ps.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", velocityColor * factor * 3);

        em.type = ParticleSystemEmissionType.Time;
        em.SetBursts(
         new ParticleSystem.Burst[] {
                      new ParticleSystem.Burst (0.0f, velocity * 50),
         });
        ps.Play();

        Destroy(hitParticle, 1.2f);

        GameObject audioSourceGO = new GameObject();

        AudioSource audioSource = audioSourceGO.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = audioMixer;

        AudioClip clip = audioClips[mappedMidiNumber];
        audioSource.PlayOneShot(clip, velocity);
        Destroy(audioSourceGO, clip.length + 0.3f);
    }

    Dictionary<int, int> mapping = new Dictionary<int, int>();
    // Start is called before the first frame update
    void Start()
    {
        print(Application.persistentDataPath);
        arduino = new SerialPort("COM11", 9600);
        arduino.Open();

        ResetRecording();
  
        statusImage.enabled = false;

        // Disable the highlight graphics
        noteHighlights.ForEach(x => {
            x.enabled = false;
         });

        mapping = new Dictionary<int, int>();
        mapping[48] = 0;
        mapping[49] = 1;
        mapping[50] = 2;
        mapping[51] = 3;
        mapping[44] = 4;
        mapping[45] = 5;
        mapping[46] = 6;
        mapping[47] = 7;


        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;

            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) => {
                // Note that you can't use note.velocity because the state
                // hasn't been updated yet (as this is "will" event). The note
                // object is only useful to specify the target note (note
                // number, channel number, device name, etc.) Use the velocity
                // argument as an input note velocity.
                //Debug.Log(string.Format(
                //    "Note On #{0} ({1}) vel:{2:0.00} ch:{3} dev:'{4}'",
                //    note.noteNumber,
                //    note.shortDisplayName,
                //    velocity,
                //    (note.device as Minis.MidiDevice)?.channel,
                //    note.device.description.product
                //));

                if(mapping.ContainsKey(note.noteNumber))
                {
                    DisplayNote(mapping[note.noteNumber], velocity);

                    if(isRecording)
                    {
                        PressData pressData = new PressData() { midiNumber = note.noteNumber, timeStamp = currentTime, velocity = velocity };
                        onPressTimestamps.Add(pressData);
                    }
                }
                if (note.noteNumber == 36)
                {
                    isRecording = !isRecording;
                    if (isRecording)
                    {
                        BeginRecord();
                    }
                    else
                    {
                        EndRecord();
                    }
                }

                if (note.noteNumber == 38)
                {
                    PlayRecorded();
                }
            };

            midiDevice.onWillNoteOff += (note) => {
                if (mapping.ContainsKey(note.noteNumber))
                {
                    noteHighlights[mapping[note.noteNumber]].enabled = false;
                    if (isRecording)
                    {
                        PressData pressData = new PressData() { midiNumber = note.noteNumber, timeStamp = currentTime, velocity = note.velocity };
                        onReleaseTimestamps.Add(pressData);
                    }

                }

                //Debug.Log(string.Format(
                //    "Note Off #{0} ({1}) ch:{2} dev:'{3}'",
                //    note.noteNumber,
                //    note.shortDisplayName,
                //    (note.device as Minis.MidiDevice)?.channel,
                //    note.device.description.product
                //));
            };
        };
    }

    void VibrateFinger(int fingerIndex, float strength)
    {
        string writeString = "";
        for(int i = 0; i < 5; i++)
        {
            if(i==fingerIndex)
            {
                writeString += Mathf.CeilToInt(strength * 8);
            }
            else
            {
                writeString += "0";
            }
        }
        arduino.Write(writeString + "\r\n");
    }

    private void OnApplicationQuit()
    {
        arduino.Close();
    }

    // Update is called once per frame
    void Update()
    {
        if(isRecording || isPlaying)
        {
            currentTime += Time.deltaTime;
        }

        // if playing back
        if (isPlaying)
        {
            if (pressNoteIndex < onPressTimestamps.Count)
            {
                PressData currentPress = onPressTimestamps[pressNoteIndex];
                if (currentTime > currentPress.timeStamp)
                {

                    pressNoteIndex++;

                    int mappedMidiNumber = mapping[currentPress.midiNumber];

                    DisplayNote(mappedMidiNumber, currentPress.velocity);

                    int finger = currentPress.finger;
                    VibrateFinger(finger, currentPress.velocity);
                }
            }
            // If at the end, stop playing
          

            PressData currentRelease = onReleaseTimestamps[releaseNoteIndex];


            if (currentTime > currentRelease.timeStamp)
            {
                releaseNoteIndex++;

                int mappedMidiNumber = mapping[currentRelease.midiNumber];

                noteHighlights[mappedMidiNumber].enabled = false;
            }

            // End playing on the final release
            if (onReleaseTimestamps.Count == releaseNoteIndex)
            {
                isPlaying = false;
                PlaybackStopped();
            }
            // If at the end, stop playing
        }
    }
}

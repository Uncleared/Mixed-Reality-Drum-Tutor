using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MidiRecorder : MonoBehaviour
{
    public class PressData
    {
        public float timeStamp = 0f;
        public float velocity;
        public int midiNumber;
    }

    public Image recordingIcon;
    public GameObject hitParticlePrefab;

    public List<MeshRenderer> noteHighlights;
    public bool isRecording = false;
    public bool isPlaying = false;

    // on press should contain several properties

    public List<PressData> onPressTimestamps;
    public List<PressData> onReleaseTimestamps;

    //int frame = 0;
    int noteIndex = 0;

    float currentTime = 0f;

    public List<AudioClip> audioClips;
    public AudioSource audioSource;
    public void BeginRecord()
    {
        ResetRecording();
        currentTime = 0f;
        recordingIcon.enabled = true;
    }

    public void EndRecord()
    {
        recordingIcon.enabled = false;
    }

    public void PlayRecorded()
    {
        isPlaying = true;
        currentTime = 0f;
        noteIndex = 0;
    }

    public void ResetRecording()
    {

        onPressTimestamps = new List<PressData>();
    }

    Dictionary<int, int> mapping = new Dictionary<int, int>();
    // Start is called before the first frame update
    void Start()
    {
        ResetRecording();
        audioSource = gameObject.AddComponent<AudioSource>();
        recordingIcon.enabled = false;

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
                Debug.Log(string.Format(
                    "Note On #{0} ({1}) vel:{2:0.00} ch:{3} dev:'{4}'",
                    note.noteNumber,
                    note.shortDisplayName,
                    velocity,
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));

                if(mapping.ContainsKey(note.noteNumber))
                {
                    MeshRenderer noteRenderer = noteHighlights[mapping[note.noteNumber]];
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

                    if(isRecording)
                    {
                        PressData pressData = new PressData() { midiNumber = note.noteNumber, timeStamp = currentTime, velocity = note.velocity };
                        onPressTimestamps.Add(pressData);
                    }

                    audioSource.PlayOneShot(audioClips[mapping[note.noteNumber]], velocity);
                    
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
                }
                Debug.Log(string.Format(
                    "Note Off #{0} ({1}) ch:{2} dev:'{3}'",
                    note.noteNumber,
                    note.shortDisplayName,
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));
            };
        };
    }

    // Update is called once per frame
    void Update()
    {
        if(isRecording || isPlaying)
        {
            currentTime += Time.deltaTime;
        }

        // if playing back
        if(isPlaying)
        {
            PressData currentPress = onPressTimestamps[noteIndex];
            if(currentTime > currentPress.timeStamp)
            {
                noteIndex++;

                int mappedMidiNumber = mapping[currentPress.midiNumber];
                MeshRenderer noteRenderer = noteHighlights[mappedMidiNumber];

                GameObject hitParticle = Instantiate(hitParticlePrefab);
                hitParticle.transform.position = noteRenderer.transform.position;
                hitParticle.SetActive(true);
                ParticleSystem ps = hitParticle.GetComponent<ParticleSystem>();
                ParticleSystem.EmissionModule em = ps.emission;
                ps.startColor = Color.red;
                ps.GetComponent<ParticleSystemRenderer>().material.EnableKeyword("_EmissionColor");
                ps.GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", Color.red * 2f);
                em.type = ParticleSystemEmissionType.Time;
                em.SetBursts(
                 new ParticleSystem.Burst[] {
                      new ParticleSystem.Burst (0.0f, 30 * 50),
                 });
                ps.Play();
                audioSource.PlayOneShot(audioClips[mappedMidiNumber]);

                Destroy(hitParticle, 1.2f);

            }
            // If at the end, stop playing
            if(onPressTimestamps.Count == noteIndex)
            {
                isPlaying = false;
            }
        }
    }
}

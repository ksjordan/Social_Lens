using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

public class ColorandDictationManipulation : MonoBehaviour, IDictationHandler
{
    public TextMesh dictationOutputText;
    private new Renderer renderer;
    public GameObject objectToBeManupulated;
    private bool isRecording;
    public TextMesh eventsList;

    //private float initialSilenceTimeout = 5f;
    // Use this for initialization

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    public void OnVoiceCommand()
    {
        Debug.Log("Started recording");
        renderer = objectToBeManupulated.GetComponent<Renderer>();
        //renderer.material.color = Color.red;
        dictationOutputText.color = Color.red;
        ToggleRecording();
    }

    public async void LoadEvents()
    {
        eventsList = GetComponent<TextMesh>();
        StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        StorageFile textFileForRead = await storageFolder.GetFileAsync("calendars.txt");
        string plainText = "";
        plainText = await FileIO.ReadTextAsync(textFileForRead);
        Debug.Log("Reading file written: " + plainText);
        eventsList.text = plainText;

    }


    public async void HideEvents()
    {
        eventsList.text = "";
    }



    private void ToggleRecording()
    {
        if (isRecording)
        {
            Debug.Log("Stopped recording in TR");
            isRecording = false;
            StartCoroutine(DictationInputManager.StopRecording());
            dictationOutputText.color = Color.red;
        }
        else
        {
            Debug.Log("Started recording in TR");
            isRecording = true;
            StartCoroutine(DictationInputManager.StartRecording(null, 10f, 20f, 10));
            dictationOutputText.color = Color.red;
            //renderer.material.color = Color.red;
        }
    }
    public void OnDictationHypothesis(DictationEventData eventData)
    {
        dictationOutputText.text = eventData.DictationResult;
    }

    public void OnDictationResult(DictationEventData eventData)
    {
        dictationOutputText.text = eventData.DictationResult;
    }
    private string result;

    public void OnDictationComplete(DictationEventData eventData)
    {
        if (result != eventData.DictationResult)
        {
            Debug.Log(eventData.DictationResult);
            dictationOutputText.text = eventData.DictationResult;
            //renderer.material.color = Color.green;
            dictationOutputText.color = Color.green;

            createCalendarEvent(eventData.DictationResult);
        }
    }

    public async void createCalendarEvent(string conv)
    {
        Debug.Log("Display within summarizeConv");
        Debug.Log(conv);

        ////string name = GetComponent<FaceAnalysis>().labelText.text;
        StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        StorageFile textFileForWrite = await storageFolder.CreateFileAsync("calendars.txt", CreationCollisionOption.OpenIfExists);
        await FileIO.AppendTextAsync(textFileForWrite, " - " + conv + "\n");
        result = conv;
        StorageFile textFileForRead = await storageFolder.GetFileAsync("calendars.txt");
        string plainText = "";
        plainText = await FileIO.ReadTextAsync(textFileForRead);
        Debug.Log("New file written: " + plainText);
    }


    public void OnDictationError(DictationEventData eventData)
    {
        isRecording = false;
        dictationOutputText.color = Color.white;
        //renderer.material.color = Color.white;
        StartCoroutine(DictationInputManager.StopRecording());
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using HoloToolkit.UX.Dialog;
using SimpleJSON;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class FaceAnalysis : MonoBehaviour {

    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static FaceAnalysis Instance;

    /// <summary>
    /// The analysis result text
    /// </summary>
    private TextMesh labelText;

    /// <summary>
    /// Bytes of the image captured with camera
    /// </summary>
    internal byte[] imageBytes;

    /// <summary>
    /// Path of the image captured with camera
    /// </summary>
    internal string imagePath;

    /// <summary>
    /// Base endpoint of Face Recognition Service
    /// </summary>
    const string baseEndpoint = "https://westus.api.cognitive.microsoft.com/face/v1.0/";

    /// <summary>
    /// Auth key of Face Recognition Service
    /// </summary>
    private const string azureKey = "69e550a6bafc449b8f90bb2c56e5d846";//"abbd309cfe8a4f8393713f14fefbfe42";////

    /// <summary>
    /// Id (name) of the created person group 
    /// </summary>
    private const string personGroupId = "maketwitter";//"sociallens";//

    /// <summary>
    /// Rendering photos on this game object
    /// </summary>
    public GameObject igObject;

    public Canvas mainCanvas;
    public GameObject instagramUIPrefab;

    public string[] handles;
    //Twitter variables
    private string twitterKey = "SfR10L97q4Soh6v7wii2vnShR";
    private string secret = "TINPY6L5pWFAW3zFKQz2T9WymDa1jVQD2az3Ym98eVgsPB43kI";
    private string accessToken;
    Twitter.TwitterUser newUser;
    Twitter.Tweet[] tweets;
    public Dialog dialogPrefab;

    /// <summary>
    /// Initialises this class
    /// </summary>
    private void Awake()
    {
        // Allows this instance to behave like a singleton
        Instance = this;

        // Add the ImageCapture Class to this Game Object
        gameObject.AddComponent<ImageCapture>();

        // Create the text label in the scene
        CreateLabel();

    }

    private void LoadTwitterContent(string twitterHandle)
    {
        accessToken = Twitter.API.GetTwitterAccessToken(twitterKey, secret);
        Debug.Log(accessToken);

        if (accessToken != null)
        {
            newUser = Twitter.API.GetProfileInfo(twitterHandle, accessToken, false);
            tweets = Twitter.API.GetUserTimeline(twitterHandle, 1, accessToken);

            if (newUser == null || tweets == null)
            {
                Debug.Log("User or Tweets is null");
                return;
            }

            for (int i = 0; i < tweets.Length; i++)
            {
                Debug.Log("Generating new Dialog game object");
                //Dialog dialog = Dialog.Open(dialogPrefab.gameObject, DialogButtonType.Close, tweets[i].user.screen_name, tweets[i].text);
            }
        }
        else
        {
            Debug.Log("Access Token is NULL!");
        }
    }

    /// <summary>
    /// Load instagram content
    /// </summary>
    private void LoadInstagramContent(string igHandle) 
    {
        string igReq = "www.instagram.com/" + igHandle;
        WWW request = new WWW(igReq);
        StartCoroutine(OnResponse(request));
    }

    private IEnumerator OnResponse(WWW req) {
        yield return req;
        Regex rgx = new Regex(@"https?:\/\/(scontent-lax3)[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)");
        MatchCollection matches = rgx.Matches(req.text);

        for(int i = 3; i < 4; i++) {
            CaptureCollection captures = matches[i].Captures;
            StartCoroutine(DownloadIGImage(captures[0].Value));
        }
    }


    private IEnumerator DownloadIGImage(string url) {
        using (WWW igImage = new WWW(url)) 
        {
            Texture2D tex;
            tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
            yield return igImage;
            igImage.LoadImageIntoTexture(tex);

            Debug.Log("Instantiating the Insta post prefab");

            GameObject instaObject = Instantiate(instagramUIPrefab, mainCanvas.transform);
            instaObject.GetComponent<RawImage>().texture = tex;

            Debug.Log("Successfully set texture");
        }
    }

    /// <summary>
    /// Spawns cursor for the Main Camera
    /// </summary>
    private void CreateLabel()
    {
        // Create a sphere as new cursor
        GameObject newLabel = new GameObject();

        // Attach the label to the Main Camera
        newLabel.transform.parent = gameObject.transform;

        // Resize and position the new cursor
        newLabel.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        newLabel.transform.position = new Vector3(0f, 3f, 60f);

        // Creating the text of the Label
        labelText = newLabel.AddComponent<TextMesh>();
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.tabSize = 4;
        labelText.fontSize = 50;
        labelText.text = ".";
    }

    /// <summary>
    /// Detect faces from a submitted image
    /// </summary>
    internal IEnumerator DetectFacesFromImage()
    {
        WWWForm webForm = new WWWForm();
        string detectFacesEndpoint = $"{baseEndpoint}detect";

        // Change the image into a bytes array
        imageBytes = GetImageAsByteArray(imagePath);

        using (UnityWebRequest www =
            UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", azureKey);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            www.uploadHandler.contentType = "application/octet-stream";
            www.uploadHandler = new UploadHandlerRaw(imageBytes);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;
            Debug.Log("csci538: " + jsonResponse);
            Face_RootObject[] face_RootObject =
                JsonConvert.DeserializeObject<Face_RootObject[]>(jsonResponse);

            List<string> facesIdList = new List<string>();
            // Create a list with the face Ids of faces detected in image
            foreach (Face_RootObject faceRO in face_RootObject)
            {
                facesIdList.Add(faceRO.faceId);
                Debug.Log($"Detected face - Id: {faceRO.faceId}");
            }

            StartCoroutine(IdentifyFaces(facesIdList));
        }
    }

    /// <summary>
    /// Returns the contents of the specified file as a byte array.
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }

    /// <summary>
    /// Identify the faces found in the image within the person group
    /// </summary>
    internal IEnumerator IdentifyFaces(List<string> listOfFacesIdToIdentify)
    {
        // Create the object hosting the faces to identify
        FacesToIdentify_RootObject facesToIdentify = new FacesToIdentify_RootObject();
        facesToIdentify.faceIds = new List<string>();
        facesToIdentify.personGroupId = personGroupId;
        foreach (string facesId in listOfFacesIdToIdentify)
        {
            facesToIdentify.faceIds.Add(facesId);
        }
        facesToIdentify.maxNumOfCandidatesReturned = 1;
        facesToIdentify.confidenceThreshold = 0.5;

        // Serialise to Json format
        string facesToIdentifyJson = JsonConvert.SerializeObject(facesToIdentify);
        // Change the object into a bytes array
        byte[] facesData = Encoding.UTF8.GetBytes(facesToIdentifyJson);

        WWWForm webForm = new WWWForm();
        string detectFacesEndpoint = $"{baseEndpoint}identify";

        using (UnityWebRequest www = UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", azureKey);
            www.SetRequestHeader("Content-Type", "application/json");
            www.uploadHandler.contentType = "application/json";
            www.uploadHandler = new UploadHandlerRaw(facesData);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;
            Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
            Candidate_RootObject[] candidate_RootObject = JsonConvert.DeserializeObject<Candidate_RootObject[]>(jsonResponse);

            // For each face to identify that ahs been submitted, display its candidate
            foreach (Candidate_RootObject candidateRO in candidate_RootObject)
            {
                StartCoroutine(GetPerson(candidateRO.candidates[0].personId));

                // Delay the next "GetPerson" call, so all faces candidate are displayed properly
                yield return new WaitForSeconds(3);
            }
        }
    }

    /// <summary>
    /// Provided a personId, retrieve the person name associated with it
    /// </summary>
    internal IEnumerator GetPerson(string personId)
    {
        string getGroupEndpoint = $"{baseEndpoint}persongroups/{personGroupId}/persons/{personId}?";
        WWWForm webForm = new WWWForm();

        using (UnityWebRequest www = UnityWebRequest.Get(getGroupEndpoint))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", azureKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;

            Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
            IdentifiedPerson_RootObject identifiedPerson_RootObject = JsonConvert.DeserializeObject<IdentifiedPerson_RootObject>(jsonResponse);

            // Display the name of the person in the UI
            labelText.text = identifiedPerson_RootObject.name;
            handles = identifiedPerson_RootObject.userData.Split('|');
            LoadTwitterContent(handles[0]);

            LoadInstagramContent(handles[1]);
            /*
            switch(identifiedPerson_RootObject.name)
            {
                case "Christin Carter":
                    LoadTwitterContent("cartercorpp");
                    break;
                case "Renu Hiremath":
                    LoadTwitterContent("renu__hiremath");
                    break;
                case "Yoofi Quansah":
                    LoadTwitterContent("yquansah_");
                    break; 
                case "Savitha Sameerdas":
                    LoadTwitterContent("savithasameer");
                    break;
                case "Ganesh Ram":
                    LoadTwitterContent("BBC");
                    break;
                case "Kristin Jordan":
                    LoadTwitterContent("kjcookies");
                    break;
                case "HariKrishna Prabhu":
                    LoadTwitterContent("realDonaldTrump");
                    break;
                    
            }*/
        }
    }
}

/// <summary>
/// The Person Group object
/// </summary>
public class Group_RootObject
{
    public string personGroupId { get; set; }
    public string name { get; set; }
    public object userData { get; set; }
}

/// <summary>
/// The Person Face object
/// </summary>
public class Face_RootObject
{
    public string faceId { get; set; }
}

/// <summary>
/// Collection of faces that needs to be identified
/// </summary>
public class FacesToIdentify_RootObject
{
    public string personGroupId { get; set; }
    public List<string> faceIds { get; set; }
    public int maxNumOfCandidatesReturned { get; set; }
    public double confidenceThreshold { get; set; }
}

/// <summary>
/// Collection of Candidates for the face
/// </summary>
public class Candidate_RootObject
{
    public string faceId { get; set; }
    public List<Candidate> candidates { get; set; }
}

public class Candidate
{
    public string personId { get; set; }
    public double confidence { get; set; }
}

/// <summary>
/// Name and Id of the identified Person
/// </summary>
public class IdentifiedPerson_RootObject
{
    public string personId { get; set; }
    public string name { get; set; }
    public string userData { get; set; }
    //public string[] handles;
}



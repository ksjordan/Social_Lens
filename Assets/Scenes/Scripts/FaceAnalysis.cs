using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using UnityEngine.Networking;
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
    public int texnum = 0;

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

    public GameObject ui;
    public GameObject tweetUI;
    public GameObject instaUI;
    public GameObject instaProfileUI;

    public Canvas mainCanvas;

    private string[] handles;

    //Instagram variables
    const int SKIP = 7;
    const int MAX_POSTS = 5;
    private string instaProfilePic;
    private string[] postUrls;
    private int instaIndex;
    private Texture2D instaProfile;
    private Texture2D[] instaImages;

    //Twitter variables
    private string twitterKey = "SfR10L97q4Soh6v7wii2vnShR";
    private string twitterKeyChris = "PnJkJydITUGMkXUifhkvAh0dM";
    private string secret = "TINPY6L5pWFAW3zFKQz2T9WymDa1jVQD2az3Ym98eVgsPB43kI";
    private string secretChris = "nQSFyNkOMBnEPh7AGBlYIKYYYpb6YWSJvH4oxzZFpa5MScCcwi";
    private string accessToken;
    private int twitterIndex;
    Twitter.TwitterUser newUser;
    Twitter.Tweet[] tweets;

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
        accessToken = Twitter.API.GetTwitterAccessToken(twitterKeyChris, secretChris);
        Debug.Log(accessToken);

        if (accessToken != null)
        {
            newUser = Twitter.API.GetProfileInfo(twitterHandle, accessToken, false);
            tweets = Twitter.API.GetUserTimeline(twitterHandle, 5, accessToken);

            if (newUser == null || tweets == null)
            {
                Debug.Log("User or Tweets is null");
                return;
            }
            twitterIndex = 0;
            Debug.Log("twitter image url: " + newUser.profile_image_url);
            StartCoroutine(DownloadTwitterImage(newUser.profile_image_url));
            InvokeRepeating("DisplayTweet", 0f, 5f);
        }
        else
        {
            Debug.Log("Access Token is NULL!");
        }
    }

    IEnumerator DownloadTwitterImage(string url)
    {
        using (WWW twitterImage = new WWW(url))
        {
            Texture2D tex;
            tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
            yield return twitterImage;
            twitterImage.LoadImageIntoTexture(tex);

            tweetUI.transform.GetChild(0).GetComponent<RawImage>().texture = tex;
        }
    }

    private void DisplayTweet()
    {
        if(tweets != null)
        {
            RawImage profileImg = tweetUI.transform.GetChild(0).GetComponent<RawImage>();
            Text fullName = tweetUI.transform.GetChild(1).GetComponent<Text>();
            Text handle = tweetUI.transform.GetChild(2).GetComponent<Text>();
            Text body = tweetUI.transform.GetChild(3).GetComponent<Text>();
            Text date = tweetUI.transform.GetChild(4).GetComponent<Text>();
            Text retweet = tweetUI.transform.GetChild(5).GetComponent<Text>();
            Text likes = tweetUI.transform.GetChild(6).GetComponent<Text>();

            fullName.text = newUser.name;
            handle.text = "@" + newUser.screen_name;
            body.text = tweets[twitterIndex].text;
            date.text = tweets[twitterIndex].created_at;
            retweet.text = tweets[twitterIndex].retweet_count.ToString();
            likes.text = tweets[twitterIndex].favorite_count.ToString();

            twitterIndex = twitterIndex >= tweets.Length - 1 ? 0 : twitterIndex + 1;
        }
        
    }

    /// <summary>
    /// Load instagram content
    /// </summary>
    private void LoadInstagramContent(string igHandle) 
    {
        Debug.Log("Insta handle: " + igHandle);
        string igReq = "www.instagram.com/" + igHandle;
        WWW request = new WWW(igReq);
        StartCoroutine(OnResponse(request));
    }

    private IEnumerator OnResponse(WWW req) {
        yield return req;
        if(req.error != null)
        {
            Debug.Log("Error: " + req.error);
        }
        Regex rgx = new Regex(@"https?:\/\/(scontent-lax3)[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)");     
        Regex followersRrgx = new Regex(@"[0-9]+ Followers");
        Regex followingRgx = new Regex(@"[0-9]+ Following");

        MatchCollection matches = rgx.Matches(req.text);
        MatchCollection followersMatches = followersRrgx.Matches(req.text);
        MatchCollection followingMatches = followingRgx.Matches(req.text);

        //Debug.Log("followers: " + followersMatches[0].Captures[0].Value);
        //Debug.Log("following: " + followingMatches[0].Captures[0].Value);
        Debug.Log("Matches size: " + matches.Count);

        if(matches.Count > 0 )
        {
            instaProfilePic = matches[0].Captures[0].Value;
            StartCoroutine(DownloadIGImage(instaProfilePic, 0));
        }

        if(followersMatches.Count > 0 )
        {
            string followers = followersMatches[0].Captures[0].Value;
            followers = followers.Substring(0, followers.Length - " Followers".Length);

            string following = followingMatches[0].Captures[0].Value;
            following = following.Substring(0, following.Length - " Following".Length);

            instaProfileUI.transform.GetChild(2).GetComponent<Text>().text = following;
            instaProfileUI.transform.GetChild(3).GetComponent<Text>().text = followers;
        }
        
        int numPosts = matches.Count >= (SKIP * MAX_POSTS + 3) ? MAX_POSTS : (matches.Count - 3) / SKIP;
        postUrls = new string[numPosts];
        instaImages = new Texture2D[numPosts];
        Debug.Log("Num posts: " + numPosts);

        //First image is always the profile pic
        

        //for (int i = 0; i < matches.Count; i++)
        //{
        //    CaptureCollection captures = matches[i].Captures;
        //    Debug.Log("pic " + i + ": " + captures[0].Value);
        //    //StartCoroutine(DownloadIGImage(captures[0].Value));
        //}

        instaIndex = 0;

        //Create list of instagram URLs
        for (int i = 0; i < numPosts; i++)
        {
            CaptureCollection captures = matches[(i * SKIP) + 3].Captures;
            postUrls[i] = captures[0].Value;
            Debug.Log("added pic: " + postUrls[i]);
            StartCoroutine(DownloadIGImage(postUrls[i], i));
        }
        
        if(numPosts > 0)
        {
            InvokeRepeating("DisplayInstaPic", 0f, 5f);
        }
    }

    private void DisplayInstaPic()
    {
        if(instaImages[instaIndex] != null)
        {
            instaUI.GetComponent<RawImage>().texture = instaImages[instaIndex];
        }
        
        instaIndex = instaIndex >= postUrls.Length - 1 ? 0 : instaIndex + 1;
        //StartCoroutine(DownloadIGImage(postUrls[instaIndex]));
    }

    private IEnumerator DownloadIGImage(string url, int index) {
        using (WWW igImage = new WWW(url)) 
        {
            Texture2D tex;
            tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
            yield return igImage;
            igImage.LoadImageIntoTexture(tex);
            instaImages[index] = tex;

            if(index == 0)
            {
                instaProfile = tex;
                instaProfileUI.transform.GetChild(0).GetComponent<RawImage>().texture = tex;
            }

            //GameObject instaObject = Instantiate(instagramUIPrefab, mainCanvas.transform);
            //instaUI.GetComponent<RawImage>().texture = tex;

            //Debug.Log("Successfully set texture");
            //instaIndex = instaIndex >= postUrls.Length - 1 ? 0 : instaIndex + 1;
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
        newLabel.transform.position = new Vector3(-16f, 9f, 60f);

        // Creating the text of the Label
        labelText = newLabel.AddComponent<TextMesh>();
        labelText.anchor = TextAnchor.UpperLeft;
        labelText.alignment = TextAlignment.Left;
        labelText.tabSize = 4;
        labelText.fontSize = 50;
        labelText.text = ".";
    }

    /// <summary>
    /// Detect faces from a submitted image
    /// </summary>
    internal IEnumerator DetectFacesFromImage()
    {
        ui.SetActive(false);
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
                yield return new WaitForSeconds(5);
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

            CancelInvoke();

            LoadInstagramContent(handles[1]);
            LoadTwitterContent(handles[0]);
            ui.SetActive(true);
        }
    }
    public void LaunchProjectile()
    {
        // Texture2D tex;
        // RawImage instaPic = instaUI.GetComponent<RawImage>();
        // RawImage profileImg = tweetUI.transform.GetChild(0).GetComponent<RawImage>();
        // Text fullName = tweetUI.transform.GetChild(1).GetComponent<Text>();
        // Text handle = tweetUI.transform.GetChild(2).GetComponent<Text>();
        // Text body = tweetUI.transform.GetChild(3).GetComponent<Text>();
        // Text date = tweetUI.transform.GetChild(4).GetComponent<Text>();
        // Text retweet = tweetUI.transform.GetChild(5).GetComponent<Text>();
        // Text likes = tweetUI.transform.GetChild(6).GetComponent<Text>();

        // switch (handles[0])
        // {
        //     case "cartercorpp":
        //         fullName.text = "Christin Carter";
        //         handle.text = "@cartercorpp";
        //         body.text = "Hello Twitter! #myfirstTweet";
        //         date.text = "12:44 AM - 29 Nov 2018";
        //         retweet.text = "0";
        //         likes.text = "0";
        //         if (texnum == 1)
        //         {
        //             tex = Resources.Load("Images/ChrisPic") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 2)
        //         {
        //             tex = Resources.Load("Images/ChrisPic2") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 3)
        //         {
        //             tex = Resources.Load("Images/ChrisPic3") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 4)
        //         {
        //             tex = Resources.Load("Images/ChrisPic4") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 5)
        //         {
        //             tex = Resources.Load("Images/ChrisPic5") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         break;
        //     case "renu__hiremath":
        //         LoadTwitterContent("renu__hiremath");
        //         if (texnum == 1)
        //         {
        //             tex = Resources.Load("Images/RenuPic") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 2)
        //         {
        //             tex = Resources.Load("Images/RenuPic2") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 3)
        //         {
        //             tex = Resources.Load("Images/RenuPic3") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 4)
        //         {
        //             tex = Resources.Load("Images/RenuPic4") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 5)
        //         {
        //             tex = Resources.Load("Images/RenuPic5") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         break;
        //     case "yquansah_":
        //         fullName.text = "Yoofi Quansah";
        //         handle.text = "@yquansah_";
        //         body.text = "There’s really people salty we got lebron 😂😂😂😂 that’s amazing. Take your hate somewhere else bro we don’t need that in LA. We got the best player in the world fam WE LIVE #Showtime";
        //         date.text = "5:36 PM - 1 Jul 2018";
        //         retweet.text = "1";
        //         likes.text = "7";
        //         tex = Resources.Load("Images/YoofiPic") as Texture2D;
        //         instaPic.texture = tex;
        //         break;
        //     case "savithasameer":
        //         fullName.text = "Savitha Sameerdas";
        //         handle.text = "@SavithaSameer";
        //         body.text = "#vmwarecodehouse #serverless #STEM Amazing weekend indeed!";
        //         date.text = "10:28 PM - 30 Jul 2018";
        //         retweet.text = "1";
        //         likes.text = "1";
        //         tex = Resources.Load("Images/SavithaPic") as Texture2D;
        //         instaPic.texture = tex;
        //         break;
        //     case "kjcookies":
        //         fullName.text = "Kristin Jordan";
        //         handle.text = "@kjcookies";
        //         //body.text = "Woah";
        //         //date.text = "8:04 AM - 29 Nov 2018";
        //         //retweet.text = "0";
        //         //likes.text = "0";
        //         Debug.Log(texnum);
        //         if (texnum == 1)
        //         {
        //             body.text = "Woah";
        //             date.text = "8:04 AM - 29 Nov 2018";
        //             retweet.text = "0";
        //             likes.text = "0";
        //             tex = Resources.Load("Images/KristinPic") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 2)
        //         {
        //             body.text = "WATCH: #Hyolyn Turns Heads In Sexy “Dally” MV Featuring Gray https://www.soompi.com/2018/04/23/watch-hyolyn-turns-heads-sexy-dally-mv-featuring-gray/ …";
        //             date.text = "2:09 AM - 23 Apr 2018";
        //             retweet.text = "0";
        //             likes.text = "0";
        //             tex = Resources.Load("Images/KristinPic2") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 3)
        //         {
        //             tex = Resources.Load("Images/KristinPic3") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 4)
        //         {
        //             tex = Resources.Load("Images/KristinPic4") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         else if (texnum == 5)
        //         {
        //             tex = Resources.Load("Images/KristinPic5") as Texture2D;
        //             instaPic.texture = tex;
        //         }
        //         //tex = Resources.Load("Images/KristinPic") as Texture2D;
        //         //instaPic.texture = tex;
        //         break;

        // }
        if (texnum == 5)
        {
            texnum = 1;
        }
        else
        {
            texnum += 1;
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



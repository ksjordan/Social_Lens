using HoloToolkit.UX.Dialog;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class twitter_web_requests : MonoBehaviour 
{
    private const string URL = "www.google.com";
    public string key = "SfR10L97q4Soh6v7wii2vnShR";
    public string secret = "TINPY6L5pWFAW3zFKQz2T9WymDa1jVQD2az3Ym98eVgsPB43kI";
    public string accessToken;
    [SerializeField] Twitter.TwitterUser newUser;
    [SerializeField] Twitter.Tweet[] tweets;
    public Text textObj;

    //UI
    public Dialog dialogPrefab;


    void Start() {
        accessToken = Twitter.API.GetTwitterAccessToken(key, secret);
        Debug.Log(accessToken);

        if(accessToken != null)
        {
            newUser = Twitter.API.GetProfileInfo("BBC", accessToken, false);
            tweets = Twitter.API.GetUserTimeline("BBC", 6, accessToken);

            if(newUser == null || tweets == null)
            {
                Debug.Log("User or Tweets is null");
                return;
            }

            for (int i = 0; i < 1; i++)
            {
                Debug.Log("Generating new Dialog game object");
                Dialog dialog = Dialog.Open(dialogPrefab.gameObject, DialogButtonType.Next|DialogButtonType.Close, tweets[i].user.screen_name, tweets[i].text);
            }
        }
        else
        {
            Debug.Log("Access Token is NULL!");
        }
        
    }

    void Update() {

    }

}

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

    void Start() {
       accessToken = Twitter.API.GetTwitterAccessToken(key, secret);
       Debug.Log(accessToken);
       newUser = Twitter.API.GetProfileInfo("BBC", accessToken, false);
       tweets = Twitter.API.GetUserTimeline("BBC", 6, accessToken);
       for (int i = 0; i < tweets.Length; i++) {
           if(textObj) {
            textObj.text = tweets[i].text;
           }
       }
    }

    void Update() {

    }

}

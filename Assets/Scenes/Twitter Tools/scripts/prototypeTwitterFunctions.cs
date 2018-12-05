using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class prototypeTwitterFunctions : MonoBehaviour
{
	//Singleton to allow easy access to this script instance
	public static prototypeTwitterFunctions instance;
	[Header("UI references")]
	public InputField usernameInput, QueryInput;
	public Slider tweetNumberSlider, tweetNumberSliderQuery;
	public Text tweetNumberText, tweetNumberTextQuery, UItoken;
	public Image UserAvatar;

	[Header("Data retrieved")]
	public Twitter.TwitterUser user;
	public string[] IDs;
	public Twitter.Tweet[] tweets;

	[Header("API Access variables")]
	public string consumerKey;
	public string consumerSecret, AccessToken;

	void Awake ()
	{
		instance = this;
		GetAccessToken();
	}

	public void GetAccessToken ()
	{
		if (!string.IsNullOrEmpty(consumerKey) && !string.IsNullOrEmpty(consumerSecret))
			AccessToken = Twitter.API.GetTwitterAccessToken(consumerKey,consumerSecret);
		else {
			if (!string.IsNullOrEmpty(consumerSecret))
				Debug.Log("Missing Consumer secret");
			if (!string.IsNullOrEmpty(consumerKey))
				Debug.Log("Missing Consumer key");
		}
	}

	void Update ()
	{
		UItoken.color = (string.IsNullOrEmpty(AccessToken) ? Color.red : Color.green);
		UItoken.text = (string.IsNullOrEmpty(AccessToken) ? "Access Token missing" : "Access Token acquired");
		tweetNumberText.text = "" + tweetNumberSlider.value;
		tweetNumberTextQuery.text = "" + tweetNumberSliderQuery.value;
		//Sometimes the instance value can be reset while editing scripts whilst in play mode from the editor
		if (!instance)
			instance = this;		
	}

	public void GetProfile ()
	{
		if (!string.IsNullOrEmpty(AccessToken)) {
			user = Twitter.API.GetProfileInfo(usernameInput.text,AccessToken,false);

			if (user != null) {
				Texture2D ava = Twitter.helperFunctions.GetTextureFromImageURL(user.profile_image_url);

				UserAvatar.sprite = Sprite.Create(
					ava,
					new Rect (0, 0, ava.width, ava.height),Vector2.one / 2
				);
			}
			else
				Debug.Log("User not found");
		}
		else
			Debug.Log("No access token :(");
	}

	public void GetTweetsFromUserTimeline ()
	{
		if (!string.IsNullOrEmpty(AccessToken))
			tweets = Twitter.API.GetUserTimeline(usernameInput.text,(int)tweetNumberSlider.value,AccessToken);
		else
			Debug.Log("No access token :(");
	}

	public void GetFollowerIDs ()
	{
		if (!string.IsNullOrEmpty(AccessToken)) {
			IDs = Twitter.API.GetIDs(usernameInput.text,Twitter.API.IDType.followers,AccessToken,500);
			if (IDs != null)
				Debug.Log(IDs.Length + " IDs collected from " + usernameInput.text + "'s followers");
			else
				Debug.Log("No ID's found");
		}
		else
			Debug.Log("No access token :(");
	}

	public void GetFriendIDs ()
	{
		if (!string.IsNullOrEmpty(AccessToken)) {
			IDs = Twitter.API.GetIDs(usernameInput.text,Twitter.API.IDType.friends,AccessToken);
			if (IDs != null)
				Debug.Log(IDs.Length + " IDs collected from " + usernameInput.text + "'s friends");
			else
				Debug.Log("No ID's found");
		}
		else
			Debug.Log("No access token :(");
	}

	public void GetRetweeters ()
	{
		if (!string.IsNullOrEmpty(AccessToken)) {
			//For sake of example
			string tweetID = "790303937794416641";

			IDs = Twitter.API.GetIDs(tweetID,Twitter.API.IDType.retweeters,AccessToken);
			if (IDs != null)
				Debug.Log(IDs.Length + " retweeter IDs collected from " + tweetID);
			else
				Debug.Log("No ID's found");
		}
		else
			Debug.Log("No access token :(");
	}

	public void ButWhatDoIDo ()
	{
		Application.OpenURL("coming soon");
	}

	public void SearchTwitter ()
	{
		//Check if we have an access token
		if (!string.IsNullOrEmpty(AccessToken)) {
			tweets = Twitter.API.SearchForTweets(QueryInput.text,AccessToken,(int)tweetNumberSliderQuery.value,Twitter.API.SearchResultType.popular);
			if (tweets != null)
				Debug.Log(tweets.Length + " tweets found");
			else
				Debug.Log("No tweets found");
		}
		else
			Debug.Log("No access token :(");
	}

}

#if UNITY_EDITOR
[CustomEditor(typeof(prototypeTwitterFunctions))]
public class PrototypeInspectorButtons : Editor
{
	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector();
		prototypeTwitterFunctions t = (prototypeTwitterFunctions)target;
		if (GUILayout.Button("Register Twitter App")) {
			Application.OpenURL("http://apps.twitter.com");
		}

		if (GUILayout.Button("Open REST API documentation")) {
			Application.OpenURL("http://dev.twitter.com/rest/reference");
		}

		if (GUILayout.Button("Get access token")) {
			t.GetAccessToken();
		}
	}
}
#endif

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Collections.Generic;

//For use interacting with the Twitter API
namespace Twitter
{
	#region some classes to store collected data in

	[System.Serializable]
	public class Twitter_DateTime
	{
		//This class is made to store all the individual time settings of a tweet/profile creation time
		public int Second;
		public int Minute;
		public int Hour;
		public string Offset;
		public int Day;
		public string Weekday;
		public string Month;
		public int Year;
	}

	public class data
	{
		//Profile/Tweet creation date
		public string created_at;
		//Formated date time
		public Twitter_DateTime FormatedDateTime;
		//The ID of the user of tweet
		public string id;
		//Language
		public string lang;

		public void FormatCreationTime ()
		{
			char[] delim = { ' ', ':' };
			string[] chunks = created_at.Split(delim);

			FormatedDateTime.Weekday = chunks [0];
			FormatedDateTime.Month = chunks [1];
			FormatedDateTime.Day = int.Parse(chunks [2]);
			FormatedDateTime.Hour = int.Parse(chunks [3]);
			FormatedDateTime.Minute = int.Parse(chunks [4]);
			FormatedDateTime.Second = int.Parse(chunks [5]);
			FormatedDateTime.Offset = chunks [6];
			FormatedDateTime.Year = int.Parse(chunks [7]);
		}
	}

	public class classTweetBase : data
	{
		//The text body of a given tweet
		public string text;
		//public bool truncated;
		public string source;
		//Make sure this isn't change to an int since these regularly exceed the int32 limit
		public string in_reply_to_status_id;
		public string in_reply_to_user_id;
		public string in_reply_to_screen_name;
		public TwitterUser user;
		public string geo;
		public string coordinates;
		public string place;
		public string contributors;
		public bool is_quote_status;
		//How many times this tweet has been retweeted
		public int retweet_count;
		//How many times this tweet has been favourited
		public int favorite_count;
		public bool retweeted;
		public bool favourited;
	}

	[System.Serializable]
	public class Tweet : classTweetBase
	{
		public Retweet retweeted_status;
	}

	[System.Serializable]
	public class Retweet : classTweetBase
	{

	}

	[System.Serializable]
	public class something
	{
		public TwitterUser[] users;
	}

	[System.Serializable]
	public class TwitterUser : data
	{
		//User's display name
		public string screen_name;
		//The user's actual username
		public string name;
		//Location entered from user bio
		public string profile_location;
		//User biography
		public string description;
		//User's website
		public string url;
		//User's follower count
		public int followers_count;
		//Number of other users this person is following
		public int friends_count;
		//Number of favourited tweets
		public int favourites_count;
		//The offset of this users timezone to standard UTC time (in minutes)
		public string utc_offset;
		//Name of timezone eg. London
		public string time_zone;
		//Does this user have geo-location enabled?
		public bool geo_enabled;
		//Is the user verfied (that fancy blue tick all the cool guys have)
		public bool verified;
		//Number of tweets
		public int statuses_count;
		//The last tweet from the users profile (not counting pinned tweets)
		public Tweet status;
		//Backgrond colour of the user's profile page
		public string profile_background_color;
		//URL of user avatar
		public string profile_image_url;
		//URL of user banner
		public string profile_banner_url;
	}

	#endregion

	#region helper functions
	public class helperFunctions
	{
		public static Texture2D GetTextureFromImageURL (string url)
		{
			WWW web = new WWW (url);
			while (!web.isDone) {
				Debug.Log("Downloading image...");
			}
			if (web.error != null)
				Debug.Log(web.error);
			else
				Debug.Log("Avatar downloaded");

			return web.texture;
		}

		public static Tweet[] GetTweetsFromString (string input)
		{
			//Split the reponse up into individual tweets
			//The time a tweet was created at will always be the start of the data set, so use that as the poin to split up the reponse string
			string[] delim = { ",{\"created_at\"" };
			string[] turnMeIntoTweets = input.Split(delim,StringSplitOptions.None);

			Tweet[] output = new Tweet[turnMeIntoTweets.Length];

			for (int i = 0; i < turnMeIntoTweets.Length; i++) {
				//Little fix for edge cases, make sure we don't break everything with bad strings
				if (!turnMeIntoTweets [i].StartsWith("{\"created_at\""))
					turnMeIntoTweets [i] = "{\"created_at\"" + turnMeIntoTweets [i];
				try {
					output [i] = JsonUtility.FromJson<Tweet>(turnMeIntoTweets [i]);
					output [i].FormatCreationTime();
					if (output [i].retweeted_status.created_at != null)
						output [i].retweeted_status.FormatCreationTime();
				}
				catch {
					Debug.Log("JSON failed in reading tweet");
					Debug.Log(input);
				}
			}
			return output;
		}
	}
	#endregion

	public class API
	{
		//Username used in the most recent API call
		private static string currentDisplayName;

		#region API AUTH

		//Authorization set-up
		public static string GetTwitterAccessToken (string consumerKey, string consumerSecret)
		{
			//Convert the consumer key and secret strings into a format to be added to our header
			string URL_ENCODED_KEY_AND_SECRET = Convert.ToBase64String(Encoding.UTF8.GetBytes(consumerKey + ":" + consumerSecret));

			byte[] body;
			body = Encoding.UTF8.GetBytes("grant_type=client_credentials");

			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["Authorization"] = "Basic " + URL_ENCODED_KEY_AND_SECRET;

			//Send a request to the Twitter API for an access token
			WWW web = new WWW ("https://api.twitter.com/oauth2/token", body, headers);
			while (!web.isDone) {
				Debug.Log("Retrieving access token...");
			}
			if (web.error != null) {
				//If there was a problem with the request, output the error to the debug log
				Debug.Log("Web error: " + web.error);
			}
			else {
				Debug.Log("Access token retrieved successfully");
				//Format string response into something more useable.
				string output = web.text.Replace("{\"token_type\":\"bearer\",\"access_token\":\"","");
				output = output.Replace("\"}","");
				return output;
			}
			//In the event of failure
			return null;
		}

		#endregion

		public enum IDType
		{
			//Those who follow the user in question
			followers,
			//Those whom the user follows
			friends,
			//Users who follow the user in question
			retweeters}
		;

		static string WebRequest (string URL, string token)
		{
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["Authorization"] = "Bearer " + token;
			WWW web = new WWW (URL, null, headers);
			while (!web.isDone) {
				Debug.Log("Processing request...");
			}

			//We have an error
			if (web.error != null) {
				//Output error
				Debug.Log(web.error);
				return null;
			}
			else {
				Debug.Log("Process completed");
				return web.text;
			}
		}

		#region API methods

		public static TwitterUser GetProfileInfo (string name, string AccessToken, bool isID)
		{
			string s = WebRequest("https://api.twitter.com/1.1/users/show.json?" + (isID ? "user_id=" : "screen_name=") + name + "&include_entities=false",AccessToken);

			TwitterUser output = JsonUtility.FromJson<TwitterUser>(s);
			if (output != null) {
				output.FormatCreationTime();
				if (output.status.created_at != null) {
					output.status.FormatCreationTime();
					if (output.status.retweeted_status.created_at != null)
						output.status.retweeted_status.FormatCreationTime();
				}
				if (output.screen_name == " ")
					output.screen_name = "Somebody with a non-ascii name";
				Debug.Log(output.screen_name + " profile retrieved");
				return output;
			}
			return null;
		}

		public static void GetMembersFromList (string accesstoken, string screen_name)
		{
			string s = WebRequest("http://api.twitter.com/1.1/lists.members.json?slug=team&owener_screen_name=" + screen_name + "&cursor=-1",accesstoken);

			if (!string.IsNullOrEmpty(s)) {
				Debug.Log("This was a success!");
			}
			else
				Debug.Log("API call failed :(");

			//Hey you can stop reading this now I'm done

			//I'm not writing anything intersting

		}

		public static Twitter.something  newFunction (string token)
		{
			string s = WebRequest("https://api.twitter.com/1.1/lists/members.json?slug=team&owner_screen_name=twitterapi&cursor=-1",token);

			Debug.Log(s);

			return JsonUtility.FromJson<Twitter.something>(s);

		}

		public static Tweet[] GetUserTimeline (string user, int amount, string token)
		{
			string input = WebRequest("https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name=" + user + "&count=" + amount + "&include_rts=true",token);
			if (!string.IsNullOrEmpty(input)) {
				//Remove the [ and ] characters that encapsulate the web response
				input = input.Remove(0,1);
				input = input.Remove(input.Length - 1,1);
				return helperFunctions.GetTweetsFromString(input);
			}
			else {
				Debug.Log("Web response failed");
			}
			return null;
		}

		public static Tweet GetTweetByID (string ID, string token)
		{
			Tweet t = JsonUtility.FromJson<Tweet>(
				          WebRequest("https://api.twitter.com/1.1/statuses/show.json?id=" + ID,token)
			          );
			t.FormatCreationTime();
			if (t.retweeted_status.created_at != null)
				t.retweeted_status.FormatCreationTime();
			return t;
		}

		public static string[] GetIDs (string name, IDType category, string AccessToken, int amount = 5000)
		{
			if (category != IDType.retweeters) {
				string response = WebRequest("https://api.twitter.com/1.1/" + category.ToString() + "/ids.json?screen_name=" + name + "&count=" + amount,AccessToken);

				if (!string.IsNullOrEmpty(response)) {
					response = response.Remove(0,8);
					response = response.Remove(response.IndexOf(']',response.Length - response.IndexOf(']')));
					return response.Split(',');
				}
			}
			else {
				//Basically do the same, 
				string response = WebRequest("https://api.twitter.com/1.1/statuses/retweeters/ids.json?id=" + name + "&count=" + amount,AccessToken);

				if (!string.IsNullOrEmpty(response)) {
					response = response.Remove(0,8);
					response = response.Remove(response.IndexOf(']',response.Length - response.IndexOf(']')));
					return response.Split(',');
				}
			}
			return null;
		}

		public enum SearchResultType
		{
			//Most recent tweets
			recent,
			//Most popular tweets
			popular,
			//A mix of both popular and recent tweets
			mixed}

		;

		public static Tweet[] SearchForTweets (string querey, string accesstoken, int amount = 1, SearchResultType searchType = SearchResultType.mixed)
		{
			string input = WebRequest("https://api.twitter.com/1.1/search/tweets.json?q=" + WWW.EscapeURL(querey) + "&count=" + amount + "&include_entites=false",accesstoken);
			if (!string.IsNullOrEmpty(input)) {
				
				//Debug.Log(input);
				//Remove the start and end bits of the querey result that encapsulate the tweets
				input = input.Remove(0,13);
				input = input.Remove(input.IndexOf("],\"search_metadata\""),input.Length - input.IndexOf("],\"search_metadata\""));

				//I will eventually do something super fancy with the end of this querey stuff, but not for now.
				//Also do a thing, only retrieve new tweets
				//Debug.Log(input);

				return helperFunctions.GetTweetsFromString(input);
			}
			else
				return null;
		}

		#endregion
	}
}
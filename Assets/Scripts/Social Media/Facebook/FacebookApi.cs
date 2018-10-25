using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;

public class FacebookApi : MonoBehaviour
{
    public GameObject plane;

    // Use this for initialization
    void Start()
    {

    }

    //Init FB api
    void Awake()
    {
        Debug.Log("Looking for cube thingy");
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            Debug.Log("Successfully Initialized the Facebook SDK");

            // Signal an app activation App Event
            FB.ActivateApp();

            //Login and get permissions to user's FB account
            FB.LogInWithReadPermissions(
                new List<string>() { "public_profile", "user_posts" },
                LoginCallback
            );
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void LoginCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            // AccessToken class will have session details
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            // Print current access token's User ID
            Debug.Log(aToken.UserId);
            // Print current access token's granted permissions
            foreach (string perm in aToken.Permissions)
            {
                Debug.Log(perm);
            }

            //get 3 most recent posts
            FB.API(
                "/me/posts?fields=created_time,message,full_picture,picture&limit=25",
                HttpMethod.GET,
                PostsCallback
            );
        }
        else
        {
            Debug.Log("User cancelled login");
        }
    }

    private void PostsCallback(IGraphResult result)
    {
        if (result.Error == null)
        {
            Debug.Log("Result: " + result.RawResult);

            FBGraphResult graphObject = FBGraphResult.CreateFromJSON(result.RawResult);
            if (graphObject != null)
            {
                Debug.Log("Created a graphObject");
                Debug.Log("num posts: " + graphObject.data.Count);
                for(int i = 0; i < 3; i++)
                {
                    Debug.Log("id: " + graphObject.data[i].id + ", message: " +
                        graphObject.data[i].message + " full pic: " +
                        graphObject.data[i].full_picture + " pic: " +
                        graphObject.data[i].picture);

                    if(graphObject.data[i].picture != "")
                    {
                        IEnumerator coroutine = DownloadFBImage(graphObject.data[i].picture);
                        StartCoroutine(coroutine);
                    }
                }
            }
        }

        FB.LogOut();

    }

    IEnumerator DownloadFBImage(string url)
    {
        using (WWW fbImage = new WWW(url))
        {
            Texture2D tex;
            tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
            // Wait for download to complete
            yield return fbImage;
            fbImage.LoadImageIntoTexture(tex);
            // assign texture
            plane.GetComponent<Renderer>().material.mainTexture = tex;

            //makue sure to delete textures if not using LoadImageIntoTexture!
        }
    }
}

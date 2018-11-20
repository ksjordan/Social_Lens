using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HtmlAgilityPack;
using SimpleJSON;

public class twitter_web_requests : MonoBehaviour 
{
    private const string URL = "www.instagram.com/champagnepapi";
    public void Request () {
        WWW request = new WWW(URL);
        StartCoroutine(OnResponse(request));
    }

    private IEnumerator OnResponse(WWW req) {
        yield return req;
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(req.text);
        var nodes = htmlDoc.DocumentNode
           .SelectNodes("//script[@type='text/javascript']");

        var jsonString = nodes[3].ChildNodes[0].InnerHtml;
        var jsonObj = JSON.Parse(jsonString.Substring(21));
        var subIndex = jsonObj["entry_data"]["ProfilePage"][0];
        var stringPhotos = subIndex["graphql"]["user"]["edge_owner_to_timeline_media"]["count"].Value;
        var realIndex = subIndex["graphql"]["user"]["edge_owner_to_timeline_media"]["edges"];

        int numOfPhotos = int.Parse(stringPhotos);
        for(int i = 0; i < numOfPhotos; i++) {
            Debug.Log(realIndex[i]["node"]["display_url"].Value);
        }
    }

    void Start() {
        Request();
    }
}

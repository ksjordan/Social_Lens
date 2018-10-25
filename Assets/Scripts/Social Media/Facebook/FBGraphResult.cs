using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FBGraphResult
{
    public List<FBPost> data;
    public Paging paging;

    public static FBGraphResult CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<FBGraphResult>(jsonString);
    }
}

[System.Serializable]
public class Paging
{
    public string previous;
    public string next;

    public static Paging CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Paging>(jsonString);
    }
}

[System.Serializable]
public class FBPost
{
    public string id;
    public string created_time;
    public string message;
    public string full_picture;
    public string picture;

    public static FBPost CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<FBPost>(jsonString);
    }
}
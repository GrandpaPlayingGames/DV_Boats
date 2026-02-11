using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class AudioLoader
{

    public static string AudioRoot;

    public static void Init(string modPath)
    {
        AudioRoot = Path.Combine(modPath, "Assets", "Audio");
    }

    public static string AudioPath(string fileName)
    {
        return Path.Combine(AudioRoot, fileName);
    }


    public static IEnumerator LoadMp3(
        string filePath,
        System.Action<AudioClip> onLoaded)
    {
        string url = "file:///" + filePath.Replace("\\", "/");

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

        yield return req.SendWebRequest();

        if (req.isNetworkError || req.isHttpError)
        {
            Debug.LogError("[Audio] Failed to load MP3: " + req.error);
            onLoaded?.Invoke(null);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
        onLoaded?.Invoke(clip);
    }
}



﻿using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

namespace SoundCloud
{

public class SoundCloudWWW : MonoBehaviour
{
    private const string CONNECT_URL = "https://soundcloud.com/connect/";
    private const string RESOLVE_URL = "http://api.soundcloud.com/resolve?url=";

    private const int LISTEN_PORT = 8080;

    public IEnumerator ProcessResolveURL(string url, Action<bool, string> callback)
    {
        bool success = false;
        WWW response = null;
        string resolvedURL = string.Empty;
        string request = RESOLVE_URL + url + "&client_id=" + SoundCloudConfig.CLIENT_ID;

        yield return StartCoroutine(WebRequest(request, (retVal) => response = retVal));

        if (!string.IsNullOrEmpty(response.error) && response.responseHeaders.ContainsKey("LOCATION"))
        {
            success = true;
            resolvedURL = response.responseHeaders["LOCATION"];
        }

        if (callback != null)
            callback(success, resolvedURL);
    }

    public IEnumerator WebRequest(string uri, Action<WWW> callback)
    {
        WWW www = new WWW(uri);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log(www.error);
            yield break;
        }

        if (callback != null)
            callback(www);
    }

    public IEnumerator WebRequestObject<T>(string uri, Action<T> callback) where T : DataObject<T>, new()
    {
        T target = null;
        WWW response = null;
        yield return StartCoroutine(WebRequest(uri, (retVal) => response = retVal));

        if (!string.IsNullOrEmpty(response.error))
        {
            target = new T();
            target.Deserialize(response.text);
        }

        if (callback != null)
            callback(target);
    }

    public IEnumerator WebRequestFile(string uri, string outputFilename, Action<string> callback)
    {
        string tempFile = string.Empty;
        WWW response = null;
        yield return StartCoroutine(WebRequest(uri, (retVal) => response = retVal));

        if (!string.IsNullOrEmpty(response.error))
        {
            tempFile = SoundCloud.WORKING_DIRECTORY + Path.DirectorySeparatorChar + outputFilename;
            File.WriteAllBytes(tempFile, response.bytes);
        }

        if (callback != null)
            callback(tempFile);
    }

    public IEnumerator WebRequestAudioClip(string uri, Action<AudioClip> callback)
    {
        AudioClip clip = null;
        WWW response = null;
        yield return StartCoroutine(WebRequest(uri, (retVal) => response = retVal));

        if (!string.IsNullOrEmpty(response.error))
        {
            clip = response.audioClip;
        }

        if (callback != null)
            callback(clip);
    }

    public IEnumerator AuthenticateUser()
    {
        string uriPrefix = "http://localhost:" + LISTEN_PORT + "/";
        string connectUrl = CONNECT_URL + "?";
        connectUrl += "client_id=" + SoundCloudConfig.CLIENT_ID;
        connectUrl += "&redirect_uri=" + WWW.EscapeURL(uriPrefix + "unity-game-authentication");
        connectUrl += "&response_type=code";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(uriPrefix);
        listener.Start();

        Thread authListener = new Thread(
            () =>
            {
                HttpListenerContext context = listener.GetContext();
                ProcessAuthRequest(context);
                listener.Stop();
            }
        );

        authListener.Start();
        Application.OpenURL(connectUrl);

        yield return StartCoroutine(WaitForAuthentication());
    }

    private void ProcessAuthRequest(HttpListenerContext context)
    {
        HttpListenerRequest req = context.Request;
        HttpListenerResponse res = context.Response;

        Debug.Log(req.Url);
        // TODO: Parse oauth code from url and save it to a file.

        using (Stream outputStream = res.OutputStream)
        {
            string responseString = "<HTML><BODY>Authenticated! You can now return to your game.</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            outputStream.Write(buffer, 0, buffer.Length);
        }
    }

    private IEnumerator WaitForAuthentication()
    {
        // TODO
        yield break;
    }
}

}
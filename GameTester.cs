using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public enum GameTesterMode { Production, Sandbox }
public enum GameTesterPlayerAuthenticationMode { Token, Pin }

public static class GameTester
{
    // ------------------------------------------------------------------------------------------------------ //
    // Constructor
    // ------------------------------------------------------------------------------------------------------ //
    static GameTester()
    {
        Initialized = false;
        Mode = GameTesterMode.Sandbox;
        PlayerAuthenticated = false;
        PlayerAuthenticationMode = GameTesterPlayerAuthenticationMode.Pin;
    }

    // ------------------------------------------------------------------------------------------------------ //
    // Static Data
    // ------------------------------------------------------------------------------------------------------ //
    private static Dictionary<GameTesterMode, string> serverUrls = new Dictionary<GameTesterMode, string>
    {
        { GameTesterMode.Production, "https://server.gametester.gg/dev-api/v1" },
        { GameTesterMode.Sandbox, "https://server.gametester.gg/dev-api/v1/sandbox" }
    };
    private static string serverUrl { get { return serverUrls[Mode]; } }

    // ------------------------------------------------------------------------------------------------------ //
    // Properties
    // ------------------------------------------------------------------------------------------------------ //
    public static bool Initialized { get; private set; }
    public static GameTesterMode Mode { get; private set; }

    public static bool PlayerAuthenticated { get; private set; }
    public static GameTesterPlayerAuthenticationMode PlayerAuthenticationMode { get; private set; }

    // ------------------------------------------------------------------------------------------------------ //
    // Private Fields
    // ------------------------------------------------------------------------------------------------------ //
    private static string developerToken;
    private static string playerTokenOrPin;

    // ------------------------------------------------------------------------------------------------------ //
    // Initialize
    // ------------------------------------------------------------------------------------------------------ //
    public static void Initialize(GameTesterMode mode, string developerToken)
    {
        Mode = mode;
        GameTester.developerToken = developerToken;
        Initialized = true;
    }

    // ------------------------------------------------------------------------------------------------------ //
    // Private Helper Methods
    // ------------------------------------------------------------------------------------------------------ //
    private static Dictionary<string, object> createApiObject()
    {
        var obj = new Dictionary<string, object>();

        obj.Add("developerToken", developerToken);

        if (PlayerAuthenticationMode == GameTesterPlayerAuthenticationMode.Pin)
            obj.Add("playerPin", playerTokenOrPin);
        else
            obj.Add("playerToken", playerTokenOrPin);

        return obj;
    }

    private static IEnumerator doPost(string subUrl, Dictionary<string, object> body, Action<GameTesterResponse> callback)
    {
        using (var request = new UnityWebRequest(String.Format("{0}{1}", serverUrl, subUrl), "POST"))
        {
            var sb = new StringBuilder();
            sb.Append('{');
            int index = 0;
            foreach (var prop in body)
            {
                sb.Append('"');
                sb.Append(prop.Key);
                sb.Append('"');

                sb.Append(':');
                if (prop.Value is string) 
                {
                    sb.Append('"');
                    sb.Append(prop.Value);
                    sb.Append('"');
                }
                else
                {
                    sb.Append(prop.Value);
                }

                if (index < body.Count - 1) 
                {
                    sb.Append(',');
                }
                index++;
            }
            sb.Append('}');

            var json = sb.ToString();
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
                callback(GameTesterResponse.HttpError(request.error));
            else
                callback(GameTesterResponse.ParseResponse(request.downloadHandler.text));
        }
    }

    // ------------------------------------------------------------------------------------------------------ //
    // Public Helper Methods
    // ------------------------------------------------------------------------------------------------------ //
    public static void SetPlayerPin(string pin)
    {
        playerTokenOrPin = pin;
        PlayerAuthenticationMode = GameTesterPlayerAuthenticationMode.Pin;
        PlayerAuthenticated = true;
    }

    public static void SetPlayerToken(string token)
    {
        playerTokenOrPin = token;
        PlayerAuthenticationMode = GameTesterPlayerAuthenticationMode.Token;
        PlayerAuthenticated = true;
    }

    // ------------------------------------------------------------------------------------------------------ //
    // Api
    // ------------------------------------------------------------------------------------------------------ //
    public static class Api
    {
        public static IEnumerator Auth(Action<GameTesterResponse> callback)
        {
            return doPost("/auth", createApiObject(), callback);
        }

        public static IEnumerator Datapoint(int datapointId, Action<GameTesterResponse> callback)
        {
            var body = createApiObject();
            body.Add("datapointId", datapointId);
            return doPost(string.Empty, body, callback);
        }

        public static IEnumerator UnlockTest(Action<GameTesterResponse> callback)
        {
            var body = createApiObject();
            body.Add("function", "unlock");
            return doPost(string.Empty, body, callback);
        }
    }

}

// ------------------------------------------------------------------------------------------------------ //
// Response
// ------------------------------------------------------------------------------------------------------ //

public enum GameTesterResponseCode: int
{
    HttpError = -10,
    ResponseParseError = -11,
    
    Success = -1,
    
    GeneralError = 0,
    
    MissingDeveloperToken = 1,
    MissingPlayerAuthentication = 2,
    InvalidDeveloperToken = 3,
    InvalidPlayerToken = 4,
    InvalidPlayerPin = 5,
    MissingParameters = 6,
    DataPointDoesNotExist = 7,
    TestNotRunning = 8,
    InvalidPlayerForTest = 9,
    InvalidFunctionName = 10,
    TestAlreadyUnlocked = 11,
    TestNotInSetupState = 12,
}

public struct GameTesterResponse
{
    public GameTesterResponseCode Code { get; private set; }
    public string Message { get; private set; }

    public static GameTesterResponse ParseResponse(string webResult)
    {
        try
        {
            var response = JsonUtility.FromJson<ResponseJson>(webResult);
            return new GameTesterResponse
            {
                Code = (GameTesterResponseCode)response.code,
                Message = response.message
            };
        }
        catch (Exception e)
        {
            return new GameTesterResponse
            {
                Code = GameTesterResponseCode.ResponseParseError,
                Message = e.Message
            };
        }
    }

    public static GameTesterResponse HttpError(string error)
    {
        return new GameTesterResponse
        {
            Code = GameTesterResponseCode.HttpError,
            Message = error
        };
    }

    [System.Serializable]
    public class ResponseJson
    {
        public int code;
        public string message;
    }

    public override string ToString() { return String.Format("[({0}){1}] {2}", (int)Code, Enum.GetName(typeof(GameTesterResponseCode), Code), Message); }
}

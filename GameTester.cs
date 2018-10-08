using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum GameTesterMode { Production, Test, Sandbox }
public enum GameTesterPlayerAuthenticationMode { Token, Pin }

public static class GameTester
{
    // ------------------------------------------------------------------------------------------------------ //
    // Static Data
    // ------------------------------------------------------------------------------------------------------ //
    private static Dictionary<GameTesterMode, string> serverUrls = new Dictionary<GameTesterMode, string>
    {
        { GameTesterMode.Production, "https://server.gametester.co/dev-api" },
        { GameTesterMode.Sandbox, "https://server.gametester.co/dev-api-sandbox" },
        { GameTesterMode.Test, "https://server.gametester.co/dev-api-test" },
    };
    private static string serverUrl { get { return serverUrls[Mode]; } }

    // ------------------------------------------------------------------------------------------------------ //
    // Properties
    // ------------------------------------------------------------------------------------------------------ //
    public static bool Initialized { get; private set; } = false;
    public static GameTesterMode Mode { get; private set; } = GameTesterMode.Sandbox;

    public static bool PlayerAuthenticated { get; private set; } = false;
    public static GameTesterPlayerAuthenticationMode PlayerAuthenticationMode { get; private set; } = GameTesterPlayerAuthenticationMode.Pin;

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
    private static Dictionary<string, string> createApiObject()
    {
        var obj = new Dictionary<string, string>();

        obj.Add("developerToken", developerToken);

        if (PlayerAuthenticationMode == GameTesterPlayerAuthenticationMode.Pin)
            obj.Add("playerPin", playerTokenOrPin);
        else
            obj.Add("playerToken", playerTokenOrPin);

        return obj;
    }

    private static IEnumerator doPost(string subUrl, Dictionary<string, string> body, Action<GameTesterResponse> callback)
    {
        using (var post = UnityWebRequest.Post($"{serverUrl}{subUrl}", body))
        {
            yield return post.SendWebRequest();

            if (post.isNetworkError || post.isHttpError)
                callback(GameTesterResponse.HttpError(post.error));
            else
                callback(GameTesterResponse.ParseResponse(post.downloadHandler.text));
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
            body.Add("datapointId", datapointId.ToString());
            return doPost(string.Empty, body, callback);
        }

        public static IEnumerator UnlockTest(Action<GameTesterResponse> callback)
        {
            var body = createApiObject();
            body.Add("function", "unlockPlayerTest");
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

    GeneralError = -1,
    Success = 0,

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
        catch(Exception e)
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

    private class ResponseJson
    {
        public int code;
        public string message;
    }

    public override string ToString() => $"[({(int)Code}){Enum.GetName(typeof(GameTesterResponseCode), Code)}] {Message}";
}

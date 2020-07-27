# unity-api-wrapper

Unity wrapper for the Game Tester API.

## Example Usage
The wrapper exposes a static class called GameTester.


### Initialization
```c#
private void InitializeGameTester()
{
   GameTester.Initialize(GameTesterMode.Sandbox, "developerToken");
}
```


### Player Authorization
```c#
private void Auth()
{
   // Get the playerToken or playerPin.
   ...
   
   // Set the playerPin or playerToken. This is required.
   GameTester.SetPlayerPin(playerPin);
   // OR
   GameTester.SetPlayerToken(playerToken);
   
   // Call to test if playerPin or playerToken is valid.
   // This is required. The auth call will return a playerToken that is used in subsequent calls.
   StartCoroutine(GameTester.Api.Auth(o => 
   {
      if (o.Code == GameTesterResponseCode.InvalidPlayerToken)
      {
        // Display authentication error, prevent starting the test.
      }
      else if (o.Code == GameTesterResponseCode.Success)
      {
        // Authentication success. The test can proceed.
      }
      
   }));
}
```


### Call a Datapoint
```c#
public void UpdateScore(int addition)
{
   ...
   
   if (score == 5)
   {
      StartCoroutine(GameTester.Api.Datapoint(1, o => Callback(o)));
   }
       
   ...
}
```

### Unlock a Test
Call the UnlockTest method to mark this test as complete. In this example, after the player reached level 2.
```c#
public void IncreaseLevel()
{
   ...
   
   if (level == 2)
   {
      StartCoroutine(GameTester.Api.UnlockTest(o => Callback(o)));
   }
       
   ...
}
```

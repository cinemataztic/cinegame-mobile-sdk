# CineGame Mobile SDK
SDK for creating mobile interfaces in Unity3D for games on the big silver screen

# Replication
The primary purpose of the SDK is to provide communication between the mobile interface and the host machine running the game. Many of the standard components in the SDK provide replication via "Keys" or properties so that you can trigger actions, send text or numbers at either end.

If you want to control other parameters or actions, you can do so via the _RemoteControl_ component which provides a generic interface to feed triggers and values from the host to the client, and _SendVariable_ provides a generic interface to send triggers and values from client to host.

# Haptics
The SDK supports playing preset haptic transients via the _Vibrate_ component. Haptic transients are available on iOS 13+ and Android 8.1+.

The SDK also supports playing custom haptic patterns via the same _Vibrate_ component. The haptic patterns must be defined as a standard [AHAP file](https://developer.apple.com/documentation/corehaptics/representing_haptic_patterns_in_ahap_files) for iOS and a custom csv file for Android. The files can be included as _TextAssets_ in Unity, or they can be replicated runtime from host to client.

You can use the following online tool for designing the AHAP file for iOS:
[Captain AHAP](https://ahap.fancypixel.it)

The csv file for android must follow this format:
```
PRIMITIVE_ID, {amplitude 0..1}, {delay in miliseconds}
…
#fallback:
{amplitude 0..1}, {interval in miliseconds}
…
```
The format above the #fallback line is supported on newer Android (11+) phones (A comprehensive list of PRIMITIVE_ID's can be found [here](https://developer.android.com/reference/android/os/VibrationEffect.Composition#summary)). For older phones you should provide a fallback below the #fallback line.

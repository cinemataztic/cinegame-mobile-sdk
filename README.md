# CineGame Mobile SDK
SDK for creating mobile interfaces in Unity3D for games on the big silver screen

# Replication
The primary purpose of the SDK is to provide communication between the mobile interface and the host machine running the game. Many of the standard components in the SDK provide replication via "Keys" or properties so that you can trigger actions, send text or numbers at either end.

If you want to control other parameters or actions, you can do so via the _RemoteControl_ component which provides a generic interface to feed triggers and values from the host to the client, and _SendVariable_ provides a generic interface to send triggers and values from client to host.

# Haptics
The SDK supports playing preset haptic transients via the _Vibrate_ component. Haptic transients are available on iOS 13+ and Android 8.1+.

The SDK also supports playing custom haptic patterns via the same _Vibrate_ component. The haptic patterns must be defined as a standard [AHAP file](https://developer.apple.com/documentation/corehaptics/representing_haptic_patterns_in_ahap_files) for iOS.
Consider using the [HapticSync](https://apps.apple.com/dk/app/hapticsync-vibration-studio/id6743813963) app for creating cross-platform haptic patterns. This tool is also able to export a JSON file for the Android platform which our Vibrate component can parse.

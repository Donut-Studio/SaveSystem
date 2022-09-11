# Save System Readme
Welcome!
This is my solution for saving data from your game to a file.
Feel free to use it anywere in your projects.
Cheers, Donut Studio!


***
# Features
- one class for all the data
- three saving methods (json, binary, aes)
- easy setup and usage


***
# Installation
1. download the code or .unitypackage
2. import everything into your project
3. put the data you want to save into the class
4. implement the system into your game


***
# Usage
After importing you're ready to start implementing.
Firstly, open up the `GameSave.cs` and add the data you would like to save.
Make sure they are serializable: for example a `Vector3` should be converted to a struct and classes should have the [System.Serializable] attribute!

---
Now it's time to initialize the system and start saving/loading:
```csharp
bool success = SaveSystem.Initialize(Application.persistentDataPath, "save.dat", SaveMethod.json);
```
Setting up the system with different SaveMethods can be done in the Initialize method. 
If you are using AES for saving, make sure to use a key: `byte[] key = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F };`! Available key sizes are 128, 192, and 256 bits.

---
Great! With that you can start saving and loading:
```csharp
bool success = SaveSystem.Save();
bool success = SaveSystem.Load();
```
Accessing the data can be done with this code:
```csharp
var x = SaveSystem.GameSave.variable;
```

If some errors occur while loading or saving, you can reset or delete the file:
```csharp
bool success = SaveSystem.Reset();
bool success = SaveSystem.Delete();
```

---
Your code should look something like this:
```csharp
void Start()
{
    byte[] key = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F };
    if (SaveSystem.Initialize(Application.persistentDataPath, "save.dat", SaveMethod.aes, key))
    {
        Debug.Log("Initialization successful!");

        if (SaveSystem.Load())
        {
            Debug.Log("Loading successful! x=" + SaveSystem.GameSave.x);
        }
        else
        {
            Debug.Log("Loading failed! Deleted: " + SaveSystem.Delete());
        }
    }

    SaveSystem.GameSave.x = Random.Range(0, 11);

    if (SaveSystem.Save())
    {
        Debug.Log("Saving successful! x=" + SaveSystem.GameSave.x);
    }
    else
    {
        Debug.Log("Saving failed!");
    }
}
```


***
# Credits
Save System - Extention for Unity to use advanced saving in your game.
Created by Donut Studio, September 11, 2022.
Released into the public domain.
using Godot;
using System.Diagnostics;

namespace LOM.Files;

public class Directory {
    private static bool Debugging = true;
    public static void Ensure(string path){
        if (Debugging) Debug.Print("Directory: Attempting to create directory: \"" + path + "\"");
        Error error = DirAccess.MakeDirRecursiveAbsolute(path);
        if (Debugging) Debug.Print("Directory: Error is " + error);
    }
}
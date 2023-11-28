using Godot;
using System;

public class World {
    private static string rootFolder = "user://Worlds/";
    public readonly string Name = "New World";

    public World(string worldName){
        Name = worldName;
    }

    public string GetSavePath(){
        return rootFolder + Name + "/";
    }
}
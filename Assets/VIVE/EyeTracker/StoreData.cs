using System;


public static class StoreData{
    public static String Name="N", ID="id", SceneName= "sCENE";
    public static float height = 0.8f,age = 20;// in meter

    public static void Store_Data(string name, String id, float _height = 1,float _age = 0){
        Name = name;
        ID = id;
        height = _height;
        age = _age;;
    }

    public static string GetID()
    {
        return ID;
    }

    public static string GetName()
    {
        return Name;
    }

    public static float GetHeight()
    {
        return height;
    }

    public static string GetFolderName()
    {
        return Name+"_"+ID;
    }
}

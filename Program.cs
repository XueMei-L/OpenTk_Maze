using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System.Text.Json;
using System;

public static class Program
{
    public static void Main()
    {
        var nativeWindowSetting=new NativeWindowSettings
        {
            ClientSize=new Vector2i(800,600),
            Title="OpenTk Cube",
            WindowState=WindowState.Fullscreen
        };

        using(var window=new Window(GameWindowSettings.Default,nativeWindowSetting))
        {
            window.Run();
        }
    }

    public static void _Main()
    {
        // Checks for box
        Vector3 dimensions=new Vector3(1.0f,1.0f,1.0f);
        Vector3 center=new Vector3(0.0f,0.0f,0.0f);
        CollisionBox b=new CollisionBox(center,dimensions);


        Console.WriteLine(b);
        Matrix4 Rotation=Matrix4.CreateRotationY(MathHelper.PiOver2);
        b.Transform(Rotation);
        Console.WriteLine(b);
        Matrix4 Translation=Matrix4.CreateTranslation(10.0f,0.0f,10.0f);
        b.Transform(Translation);
        Console.WriteLine(b);
        Matrix4 Scale=Matrix4.CreateScale(3.0f);
        b.Transform(Scale);
        Console.WriteLine(b);


    }
}
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PharloomFishing;

public static class ResourceUtil
{
    private const string SpritesPath = "PharloomFishing.Assets.Sprites.";
    private const string AnimationsPath = "PharloomFishing.Assets.Animations.";
    private const string AudioPath = "PharloomFishing.Assets.Audio.";
    public const string LanguagesPath = "PharloomFishing.Data.Languages.";
    public const string DataPath = "PharloomFishing.Data.";

    public enum AssetType
    {
        Default,
        Fishes,
        Craw,
        UI
    }
    
    public static Sprite LoadEmbeddedSprite(string file, float pixelsPerUnit, AssetType assetType = AssetType.Default) 
    {
        string path = $"{SpritesPath}{(assetType == AssetType.Default ? "" : $"{assetType}.")}{file}.png";
        Texture2D tex = LoadEmbeddedTexture2D(path);
        Sprite result = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        result.name = result.texture.name = path;

        return result;
    }

    public static Sprite[] LoadEmbeddedAnimation(string folder, int frames, float pixelsPerUnit, AssetType assetType = AssetType.Default)
    {
        Sprite[] animation = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            string path = $"{AnimationsPath}{(assetType == AssetType.Default ? "" : $"{assetType}.")}{folder}.{i}.png";
            Texture2D tex = LoadEmbeddedTexture2D(path);
            Sprite result = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            result.name = result.texture.name = path;
            animation[i] = result;
        }
        return animation;
    }

    private static Texture2D LoadEmbeddedTexture2D(string path)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path) 
                              ?? throw new FileNotFoundException($"Embedded resource {path} not found");
        
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        
        Texture2D result = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        result.LoadImage(buffer);

        return result;
    }
    
    public static AudioClip LoadEmbeddedAudioClip(string file, AssetType assetType = AssetType.Default)
    {
        string path = $"{AudioPath}{(assetType == AssetType.Default ? "" : $"{assetType}.")}{file}.wav";
        
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path) 
                              ?? throw new FileNotFoundException($"Embedded resource {path} not found");

        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        
        //Adapted from Jeff-Kesselman on Unity forums
        int channelCount = buffer[22];
        int frequency = BytesToInt(buffer,24);
        int pos = 12;
        while(!(buffer[pos]==100 && buffer[pos+1]==97 && buffer[pos+2]==116 && buffer[pos+3]==97)) {
            pos += 4;
            int chunkSize = buffer[pos] + buffer[pos + 1] * 256 + buffer[pos + 2] * 65536 + buffer[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;
        
        int sampleCount = (buffer.Length - pos)/2;
        if (channelCount == 2) sampleCount /= 2;
        
        float[] leftChannel = new float[sampleCount];
        float[]? rightChannel = channelCount == 2 ? new float[sampleCount] : null;
        
        int i=0;
        while (pos < buffer.Length)
        {
            leftChannel[i] = BytesToFloat(buffer[pos], buffer[pos + 1]);
            pos += 2;
            if (channelCount == 2)
            {
                rightChannel?[i] = BytesToFloat(buffer[pos], buffer[pos + 1]);
                pos += 2;
            }

            i++;
        }
        
        float[] data;
        if (channelCount == 2)
        {
            data = new float[sampleCount * 2];
            for (int j = 0; j < sampleCount; j++)
            {
                data[j * 2] = leftChannel[j];
                if (rightChannel != null) data[j * 2 + 1] = rightChannel[j];
            }
        }
        else
        {
            data = leftChannel;
        }

        AudioClip clip = AudioClip.Create(file, sampleCount, channelCount, frequency, false, false);
        clip.SetData(data, 0);

        return clip;
    }

    public static void SetAndSpawnOneShot(this AudioEvent audioEvent, AudioClip clip, Vector3 position, float pitchMin = 1f, float pitchMax = 1f)
    {
        audioEvent.Clip = clip;
        audioEvent.PitchMin = pitchMin;
        audioEvent.PitchMax = pitchMax;
        audioEvent.SpawnAndPlayOneShot(position);
    }
    
    public static AudioClip? LoadEmbeddedAudioClipOrNull(string file, AssetType assetType = AssetType.Default)
    {
        try
        {
            return LoadEmbeddedAudioClip(file, assetType);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private static float BytesToFloat(byte firstByte, byte secondByte) {
        short s = (short)((secondByte << 8) | firstByte);
        return s / 32768f;
    }
    
    private static int BytesToInt(byte[] bytes, int offset = 0){
        int value = 0;
        for(int i = 0; i < 4;i++)
        {
            value |= bytes[offset + i] << (i * 8);
        }
        return value;
    }

    public static string LoadEmbeddedText(string path)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path)
                              ?? throw new FileNotFoundException($"Embedded resource {path} not found");
        return new StreamReader(stream).ReadToEnd();
    }

    public static string? LoadEmbeddedTextOrNull(string path)
    {
        try
        {
            return LoadEmbeddedText(path);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
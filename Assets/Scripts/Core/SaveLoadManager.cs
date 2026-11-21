using System;
using System.IO;
using UnityEngine;

public sealed class SaveLoadManager : Singleton<SaveLoadManager>
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_GameEvent onCaptureSave;
    [SerializeField] private SO_GameEvent onSaveLoaded;
    [SerializeField] private SO_GameEvent onSaveRequested;
    [SerializeField] private SO_GameEvent onSaveFailed;
    [SerializeField] private int activeSlot;
    public string LastError { get; private set; }
    private void OnEnable() { if (onSaveRequested != null) onSaveRequested.RegisterListener(SaveActiveSlot); }
    private void OnDisable() { if (onSaveRequested != null) onSaveRequested.UnregisterListener(SaveActiveSlot); }
    private void SaveActiveSlot() { Save(activeSlot); }
    private static string PathFor(int slot)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
    }
    /// <summary>Captures systems through an event, then atomically writes the session as JSON.</summary>
    public void Save(int slot)
    {
        try
        {
            string path = PathFor(slot);
            if (session == null) throw new InvalidOperationException("Save session is not configured.");
            onCaptureSave?.Raise();
            if (!session.Data.IsValid()) throw new InvalidDataException("Invalid session data.");
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(session.Data, true));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
            activeSlot = slot;
            LastError = null;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException)
        { Fail(error); }
    }
    /// <summary>Loads a valid slot and notifies systems; returns null without changing the session on failure.</summary>
    public SaveData Load(int slot)
    {
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            if (session == null) throw new InvalidOperationException("Save session is not configured.");
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{") || !json.Contains("\"version\""))
                throw new InvalidDataException("Invalid save JSON.");
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || !data.IsValid()) throw new InvalidDataException("Save data is corrupt or unsupported.");
            session.Replace(data);
            activeSlot = slot;
            LastError = null;
            onSaveLoaded?.Raise();
            return data;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException)
        { Fail(error); return null; }
    }
    /// <summary>Returns whether a slot file exists.</summary>
    public bool HasSave(int slot) => slot >= 0 && File.Exists(PathFor(slot));
    /// <summary>Deletes a slot and its backup and temporary file.</summary>
    public void DeleteSave(int slot)
    {
        try
        {
            string path = PathFor(slot);
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
            LastError = null;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        { Fail(error); }
    }
    private void Fail(Exception error)
    {
        LastError = error.Message;
        Debug.LogWarning("Save/load failed: " + LastError, this);
        onSaveFailed?.Raise();
    }
}

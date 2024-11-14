using MelonLoader;
using UnityEngine;

namespace Tungsten;

[RegisterTypeInIl2Cpp]
/// A Unity component for controlling a Tungsten script
public class ScriptBehaviour : MonoBehaviour
{
    public string scriptName = "";
    private Script _script = null;

    private bool _hasUpdate;
    private bool _hasOnGUI;

    private bool _runStart = false;

    public void OnScriptAdded()
    {
        if (scriptName == "")
            return;

        _script = Script.NewOf(scriptName);
        if (_script == null) return;

        _script.OnReload += OnReload;

        if (_script.HasFunction("awake"))
            _script.Run("awake");

        _hasOnGUI = _script.HasFunction("onGUI");
        _hasUpdate = _script.HasFunction("update");
    }

    private void OnReload()
    {
        if (_script == null) return;

        if (_script.HasFunction("awake"))
            _script.Run("awake");

        _runStart = true;

        _hasOnGUI = _script.HasFunction("onGUI");
        _hasUpdate = _script.HasFunction("update");
    }

    private void Awake()
    {
        OnScriptAdded();
    }

    private void Start()
    {
        if (_script == null) return;

        if (_script.HasFunction("start"))
            _script.Run("start");
    }

    private void Update()
    {
        if (_script == null) return;

        if (_runStart)
        {
            _runStart = false;
            if (_script.HasFunction("start"))
                _script.Run("start");
        }

        if (_hasUpdate)
            _script.Run("update");
    }

    private void OnGUI()
    {
        if (_script == null) return;

        if (_hasOnGUI)
            _script.Run("onGUI");
    }
}
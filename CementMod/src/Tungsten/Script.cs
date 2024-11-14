using CementGB.Mod;
using Il2CppSystem.IO;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Tungsten;

public class Script
{
    public static readonly string scriptsPath = Path.Combine(Mod.userDataPath, "TungstenScripts");

    public bool faultyScript { get; private set; }
    public string name { get; private set; }
    private ProgramInfo programInfo;

    private static readonly Dictionary<string, ProgramInfo> scriptCache = new();
    private static readonly List<Script> _scripts = new();
    private static readonly Dictionary<string, Script> _globalScripts = new();

    public static Script[] Scripts => _scripts.ToArray();

    public event System.Action OnReload;

    private Script(string name)
    {
        this.name = name;
        programInfo = null;
        if (scriptCache[name] != null)
        {
            programInfo = scriptCache[name].Clone();
        }

        faultyScript = programInfo == null || programInfo.errorManager.HadError;

        if (programInfo != null)
            Run("__regglobal");

        _scripts.Add(this);
    }

    /// <summary>
    /// Checks for the keybind to hot reload scripts, and also does the processing of global scripts.
    /// </summary>
    public static void Update()
    {
        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.shiftKey.isPressed && Keyboard.current.rKey.wasPressedThisFrame)
            ReloadScripts();

        foreach (var global in _globalScripts.Values)
            if (global.HasFunction("update"))
                global.Run("update");
    }

    /// <summary>
    /// Runs OnGUI for all global scripts.
    /// </summary>
    public static void OnGUI()
    {
        foreach (var global in _globalScripts.Values)
            if (global.HasFunction("onGUI"))
                global.Run("onGUI");
    }

    /// <summary>
    /// Used to load and hot reload scripts.
    /// </summary>
    public static void ReloadScripts()
    {
        LoadScriptsRecursive(scriptsPath);

        foreach (var script in _scripts)
        {
            // here we technically make override new global scripts, but i can't think of a better way to do it so...
            script.programInfo = scriptCache[script.name].Clone();

            script.faultyScript = script.programInfo == null || script.programInfo.errorManager.HadError;
            script.Run("__regglobal");

            if (script.OnReload != null)
                script.OnReload();

            if (!script.faultyScript && script.programInfo.isGlobal && script.HasFunction("awake"))
                script.Run("awake");
        }
    }

    private static void LoadScriptsRecursive(string directory, string directoryAppend = "")
    {
        foreach (var scriptPath in Directory.GetFiles(directory, "*.w"))
        {
            Parser parser = new(File.ReadAllText(scriptPath));
            var programInfo = parser.Parse();
            var scriptName = Path.Combine(directoryAppend, Path.GetFileName(scriptPath)).Replace('\\', '/');

            scriptCache[scriptName] = programInfo;

            if (programInfo != null && programInfo.isGlobal && !_globalScripts.ContainsKey(scriptName))
                _globalScripts[scriptName] = new Script(scriptName);
        }

        foreach (var sub in Directory.GetDirectories(directory))
            LoadScriptsRecursive(sub, Path.Combine(directoryAppend, sub));
    }

    /// <summary>
    /// Get a new instance of the script with the input name.
    /// </summary>
    /// <param name="name">Name path of the script to create a new instance of.</param>
    /// <returns></returns>
    public static Script NewOf(string name)
    {
        if (scriptCache.ContainsKey(name))
        {
            if (scriptCache[name] != null && scriptCache[name].isGlobal)
            {
                VM.Error(-1, "Cannot create a copy of a global script. Remove '@global' directive in order to allow copies.");
                return null;
            }
            return new Script(name);
        }
        return null;
    }

    /// <summary>
    /// Checks if a script has a function with the given name defined.
    /// </summary>
    /// <param name="functionName">The name to check for.</param>
    /// <returns></returns>
    public bool HasFunction(string functionName)
    {
        return programInfo.functions.ContainsKey(functionName);
    }

    /// <summary>
    /// Gets the number of parameters a function of the script takes. Returns -1, if the function isn't found.
    /// </summary>
    /// <param name="functionName">The name of the function to check the arity of.</param>
    /// <returns></returns>
    public int GetFunctionArity(string functionName)
    {
        return !HasFunction(functionName) ? -1 : programInfo.functions[functionName].arity;
    }

    /// <summary>
    /// Run a function on the script.
    /// </summary>
    /// <param name="functionName">The name of the function to run.</param>
    /// <param name="args">A list of arguments to pass to the function.</param>
    /// <returns></returns>
    public object Run(string functionName, params object?[] args)
    {
        if (programInfo == null)
        {
            MelonLogger.Error("Script doesn't have a program loaded.");
            return null;
        }

        if (faultyScript)
        {
            programInfo.errorManager.RaiseRuntimeError(-1, "Cannot run faulty script.");
            return null;
        }

        return VM.RunFunction(programInfo, functionName, args);
    }
}

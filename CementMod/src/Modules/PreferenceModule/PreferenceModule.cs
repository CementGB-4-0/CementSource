using CementGB.Mod.Utilities;
using Il2CppGB.Platform.Utils;
using Il2CppGB.UI;
using Il2CppGB.UI.Menu;
using Il2CppGB.UI.Utils.Settings;
using Il2CppTMPro;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CementGB.Mod.Modules.PreferenceModule;

internal static class PreferenceModule
{
    public const string PrefsMenuObjName = "Mod Settings";

    private static bool setupPrefabs = false;

    private static GameObject boolOptionPrefab;
    private const string BoolOptionPath = "Managers/Menu/Settings Menu/Canvas/Input Root/Controller Vibration";

    private static GameObject intOptionPrefab;
    private const string IntOptionPath = "Managers/Menu/Settings Menu/Canvas/Root Settings/PingUI";

    internal static void Initialize()
    {
        MelonEvents.OnSceneWasLoaded.Subscribe(SceneLoaded);
    }

    private static void SceneLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Menu")
        {
            CreateMenuPrefsUI();
            return;
        }

        if (!setupPrefabs)
            SetupPrefabs();

        CreateMenuPrefsUI();
    }


    private static void SetupPrefabs()
    {
        boolOptionPrefab = SetupPrefab(BoolOptionPath);
        intOptionPrefab = SetupPrefab(IntOptionPath);
        intOptionPrefab.GetComponent<IntOptionHandler>().valueNameOverrides = null;

        setupPrefabs = true;
    }

    private static GameObject SetupPrefab(string path)
    {
        var originalObject = GameObject.Find(path);
        if (originalObject == null)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkRed, "OBJECT IS NULL");
            return null;
        }
        var prefab = GameObject.Instantiate(originalObject, GameObject.Find("Managers/Menu/Settings Menu/Canvas").transform);
        GameObject.DontDestroyOnLoad(prefab);

        RemoveLocalisation(prefab);
        RemoveLocalisation(GetTitle(prefab));

        prefab.SetActive(false);
        return prefab;
    }

    private static void RemoveLocalisation(GameObject gameObject)
    {
        var disabledIf = gameObject.GetComponent<DisabledIfPlatform>();
        if (disabledIf != null)
        {
            GameObject.Destroy(disabledIf);
        }

        var localiseStringEvent = gameObject.GetComponent<LocalizeStringEvent>();
        if (localiseStringEvent != null)
        {
            GameObject.Destroy(localiseStringEvent);
        }

        var gameObjectLocaliser = gameObject.GetComponent<GameObjectLocalizer>();
        if (gameObjectLocaliser != null)
        {
            GameObject.Destroy(gameObjectLocaliser);
        }
    }

    private static void CreateMenuPrefsUI()
    {
        var uiScreen = CreatePrefsScreen();
        var uiButton = CreatePrefsButton();

        uiButton?.onClick.AddListener(new Action(() =>
        {
            var menu = GameObject.Find("Managers/Menu").GetComponent<MenuController>();
            menu?.PushScreen(uiScreen);
        }));
    }

    private static BaseMenuScreen CreatePrefsScreen()
    {
        // TODO: Look for in-game menu & create in-game live prefs menu instead if isInGame is true

        LoggingUtilities.VerboseLog("Creating Menu screen for mod preferences. . .");
        var inputRoot = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Input Root");
        if (inputRoot == null)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow, "Could not find input controls menu. Menu screen not created.");
            return null;
        }
        var newMenu = Object.Instantiate(inputRoot, inputRoot.transform.parent, true);
        var emptyButton = newMenu.transform.Find("Reset All").GetComponent<Button>();

        Object.Destroy(emptyButton);

        CreateBoolOption(newMenu.transform, "Test1", false);
        CreateIntOption(newMenu.transform, "Test2", 2);

        Object.Destroy(newMenu.GetComponent<InputOptions>());

        var menuScreen = newMenu.GetComponent<BaseMenuScreen>();
        menuScreen.defaultSelection = emptyButton;
        menuScreen.defaultSelectionFallback = emptyButton;

        // menuScreen.cancelEvent = new BaseMenuScreen.CancelEvent();
        menuScreen.cancelEvent.AddListener(new Action(() =>
        {
            var menu = GameObject.Find("Managers/Menu").GetComponent<MenuController>();
            menu.PopScreen();
        }));
        LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Done!");

        return menuScreen;
    }

    private static GameObject GetTitle(GameObject gameObject)
    {
        return gameObject.transform.Find("HoriSort/Title").gameObject;
    }

    private static GameObject GetValue(GameObject gameObject)
    {
        return gameObject.transform.Find("HoriSort/Value").gameObject;
    }

    private static GameObject CreateButton(Action onClick)
    {
        return null;
    }

    private static GameObject CreateBoolOption(Transform parent, string name, bool currentValue)
    {
        var option = GameObject.Instantiate(boolOptionPrefab, parent);
        option.SetActive(true);

        var optionHandler = option.GetComponent<BoolOptionHandler>();
        optionHandler.currentValue = currentValue;
        optionHandler.UpdateValueField();

        GetTitle(option).GetComponent<TextMeshProUGUI>().text = name;
        return option;
    }

    private static GameObject CreateIntOption(Transform parent, string name, int currentValue)
    {
        var option = GameObject.Instantiate(intOptionPrefab, parent);
        option.SetActive(true);

        var optionHandler = option.GetComponent<IntOptionHandler>();
        optionHandler.Initialise(0, int.MinValue, int.MaxValue);
        optionHandler.currentValue = currentValue;
        optionHandler.UpdateValueField();

        GetTitle(option).GetComponent<TextMeshProUGUI>().text = name;
        return option;
    }

    private static GameObject CreateStringOption(string name, string currentValue)
    {
        return null;
    }

    private static GameObject CreateKeybindOption()
    {
        return null;
    }

    private static Button CreatePrefsButton(bool isInGame = false)
    {
        LoggingUtilities.VerboseLog("Creating RootSettingsMenu preferences button. . .");

        var menu = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings");
        if (menu == null)
        {
            LoggingUtilities.VerboseLog(ConsoleColor.DarkYellow, "Could not find root settings menu. Preferences button not created.");
            return null;
        }
        var baseButton = menu.transform.Find("Audio");
        var newButtonObj = Object.Instantiate(baseButton, menu.transform, true);

        newButtonObj.name = PrefsMenuObjName;
        newButtonObj.transform.SetSiblingIndex(baseButton.transform.GetSiblingIndex());
        newButtonObj.transform.localPosition = Vector3.up * 460;

        var audioButton = baseButton.GetComponent<Button>();
        var inputButton = menu.transform.Find("Input").GetComponent<Button>();
        var newButton = newButtonObj.GetComponent<Button>();
        audioButton.ReconstructNavigationByChildren();
        inputButton.ReconstructNavigationByChildren();
        newButton.ReconstructNavigationByChildren();

        Object.Destroy(newButtonObj.GetComponent<LocalizeStringEvent>());
        newButtonObj.GetComponent<TextMeshProUGUI>().text = PrefsMenuObjName;

        // remove all click events
        newButton.onClick = new Button.ButtonClickedEvent();
        LoggingUtilities.VerboseLog(ConsoleColor.DarkGreen, "Done!");
        return newButton;
    }
}
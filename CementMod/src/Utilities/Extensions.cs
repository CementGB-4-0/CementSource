using UnityEngine.UI;

namespace CementGB.Mod.Utilities;

public static class Extensions
{
    public static void ReconstructNavigation(this Button toChange, Button up, Button down)
    {
        Navigation nav = new()
        {
            selectOnLeft = toChange.navigation.selectOnLeft,
            selectOnRight = toChange.navigation.selectOnRight,
            selectOnUp = up ?? toChange.navigation.selectOnUp,
            selectOnDown = down ?? toChange.navigation.selectOnDown
        };

        toChange.navigation = nav;
    }

    /// <summary>
    /// Reconstructs the up and down navigation buttons based off of the button children surrounding it
    /// </summary>
    /// <param name="toChange"></param>
    public static void ReconstructNavigationByChildren(this Button toChange)
    {
        Selectable[] buttons = toChange.transform.parent.GetComponentsInChildren<Selectable>();

        Navigation nav = new();
        var sIndex = -1;

        // Stupid fucking fucker button wouldn't work with Array.IndexOf so I had to do a for loop and that saddens me
        for (var i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject == toChange.gameObject)
            {
                sIndex = i;
                break;
            }
        }

        var up = sIndex == 0 ? buttons.Length - 1 : sIndex - 1;
        var down = sIndex == buttons.Length - 1 ? 0 : sIndex + 1;

        nav.selectOnLeft = toChange.navigation.selectOnLeft;
        nav.selectOnRight = toChange.navigation.selectOnRight;
        nav.selectOnUp = buttons[up];
        nav.selectOnDown = buttons[down];

        toChange.navigation = nav;
    }
}

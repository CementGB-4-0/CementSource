using UnityEngine.UI;

namespace CementGB.Mod.Utilities;

public static class UIExtensions
{
    /// <summary>
    /// Manually reconstructs the <see cref="Navigation"/> of <paramref name="toChange"/> with the corresponding <paramref name="up"/> and <paramref name="down" /> <see cref="Selectable"/>s.
    /// </summary>
    /// <param name="toChange">The <see cref="Selectable"/> whose navigation must change.</param>
    /// <param name="up">The <see cref="Selectable"/> whose navigation must map 'up' from <paramref name="toChange"/>.</param>
    /// <param name="down">The <see cref="Selectable"/> whose navigation must map 'down' from <paramref name="toChange"/>.</param>
    public static void ReconstructNavigation(this Selectable toChange, Selectable up, Selectable down)
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
    /// Reconstructs the up and down <see cref="Navigation"/> of <paramref name="toChange"/> based off the children of type <see cref="Selectable"/> surrounding it.
    /// </summary>
    /// <param name="toChange">The <see cref="Selectable"/> whose navigation must change.</param>
    public static void ReconstructNavigationByChildren(this Selectable toChange)
    {
        var buttons = toChange.transform.parent.GetComponentsInChildren<Selectable>();
        var sIndex = -1;

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

        toChange.ReconstructNavigation(buttons[up], buttons[down]);
    }
}

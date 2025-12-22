namespace CementGB.Utilities;

public static class TypeExtensions
{
    public static bool IsCastableTo(this Type from, Type to)
    {
        return to.IsSubclassOf(from) || to.IsAssignableFrom(from) || from == to;
    }

    public static bool IsCastableTo(this Il2CppSystem.Type from, Il2CppSystem.Type to)
    {
        return to.IsSubclassOf(from) || to.IsAssignableFrom(from) || from == to;
    }
}
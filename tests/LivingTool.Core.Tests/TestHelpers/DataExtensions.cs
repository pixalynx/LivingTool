namespace LivingTool.Core.Tests.TestHelpers;

public static class DataExtensions
{
    public static T Mutate<T>(this T toMutate, Action<T> mutator) where T : class
    {
        mutator(toMutate);

        return toMutate;
    }
}
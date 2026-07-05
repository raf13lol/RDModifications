namespace RDModifications;

// I would like quick processes, not safe ones

public class UnsafeArrayUtils
{
    public static int IndexOf(int[] array, int lengthOfArray, int elementToFind, int notFoundValue = -1)
    {
        for (int i = 0; i < lengthOfArray; i++)
            if (array[i] == elementToFind)
                return i;
        return notFoundValue;
    }
}
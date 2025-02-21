using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace RDModifications;

public class AccessUtils
{
    public static MethodInfo GetMethodCalled(Type T, string name)
    {
        return AccessTools.FirstMethod(T, (m) => m.Name == name);
    }

    public static MethodInfo GetMethodStartsWith(Type T, string name)
    {
        return AccessTools.FirstMethod(T, (m) => m.Name.StartsWith(name));
    }

    public static MethodInfo GetMethodContains(Type T, string name)
    {
        return AccessTools.FirstMethod(T, (m) => m.Name.Contains(name));
    }

    public static MethodInfo GetInnerMethodContains(Type T, string inner, string name)
    {
        Type innerType = AccessTools.FirstInner(T, (t) => t.Name.Contains(inner));
        return AccessTools.FirstMethod(innerType, (m) => m.Name.Contains(name));
    }

    public static MethodInfo GetInnerMethodContainsWithArgs(Type T, string inner, string name, Type[] methodArgTypes)
    {
        Type innerType = AccessTools.FirstInner(T, (t) => t.Name.Contains(inner));
        List<MethodInfo> possibleMethods = AccessTools.GetDeclaredMethods(innerType);

        foreach (MethodInfo possibleMethod in possibleMethods)
        {
            if (!possibleMethod.Name.Contains(name))
                continue;
            ParameterInfo[] argTypes = possibleMethod.GetParameters();
            bool correctMethod = true;

            for (int i = 0; i < methodArgTypes.Length; i++)
            {
                // this length checks for indexoutofrangeexception
                if (argTypes.Length <= i || argTypes[i].ParameterType != methodArgTypes[i])
                {
                    correctMethod = false;
                    break;
                }
            }

            if (!correctMethod)
                continue;

            return possibleMethod;
        }
        return null;
    }

    public static FieldInfo GetFieldContains(Type T, string name)
    {
        FieldInfo[] possibleFields = T.GetFields();

        foreach (FieldInfo possibleField in possibleFields)
        {
            if (possibleField.Name.Contains(name))
                return possibleField;
        }
        return null;
    }
}
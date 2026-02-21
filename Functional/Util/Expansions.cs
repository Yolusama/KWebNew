using System.Reflection;

namespace Functional.Util;

public static class ObjectExpansion
{
    public static void CopyProperties(this object to,object from)
    {
        var fromType = from.GetType();
        var toType = to.GetType();
        if (fromType == toType)
        {
            var properties = fromType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
                property.SetValue(to, property.GetValue(from));
        }
        else
        {
            var fromProperties = fromType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var toProperties = toType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in toProperties)
            {
                var fromProperty = fromProperties
                    .FirstOrDefault(x => x.Name == property.Name
                                         && x.PropertyType.IsAssignableTo(property.PropertyType));
                if(fromProperty != null && fromProperty.PropertyType.IsAssignableTo(property.PropertyType))
                    property.SetValue(to, property.GetValue(from));
            }
        }
    }

    public static T2 MapTo<T1, T2>(this T1 src)
    {
        var destType = typeof(T2);
        
        var res = Activator.CreateInstance(destType);
        res.CopyProperties(src);
        
        return (T2)res;
    }
    
    public static void CopyFields(this object to,object from)
    {
        var fromType = from.GetType();
        var toType = to.GetType();
        if (fromType == toType)
        {
            var fields = fromType.GetFields(BindingFlags.Instance | BindingFlags.Public
            |BindingFlags.NonPublic);
            foreach (var field in fields)
                field.SetValue(to, field.GetValue(from));
        }
        else
        {
            var fromFields = fromType.GetFields(BindingFlags.Instance | BindingFlags.Public
            |BindingFlags.NonPublic);
            var toFields = toType.GetFields(BindingFlags.Instance | BindingFlags.Public
            |BindingFlags.NonPublic);
            foreach (var field in toFields)
            {
                var fromProperty = fromFields
                    .FirstOrDefault(x => x.Name == field.Name
                                         && x.FieldType.IsAssignableTo(field.FieldType));
                if(fromProperty != null && fromProperty.FieldType.IsAssignableTo(field.FieldType))
                    field.SetValue(to, field.GetValue(from));
            }
        }
    }
}
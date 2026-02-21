using System.Collections.Frozen;
using System.Reflection;
using Functional.Logger;

namespace KWeb.DependencyInjection;

public class ServiceProvider : IServiceProvider
{
    private readonly Dictionary<string, Func<object>> scopedObjs;
    private readonly Dictionary<string, object> singleObjs;
    private readonly List<IDisposable> releasingObjs;
    
    private FrozenDictionary<string,Func<object>> ScopedObjs => scopedObjs.ToFrozenDictionary();
    private FrozenDictionary<string,object> SingleObjs => singleObjs.ToFrozenDictionary();

    public ServiceProvider()
    {
        scopedObjs = new Dictionary<string, Func<object>>();
        singleObjs = new Dictionary<string, object>();
        releasingObjs = new List<IDisposable>();
    }
    public object? Get(Type type, string name = "")
    {
        object? instance = null;
        string serviceName = name == "" ? type.Name : name;
        if (ScopedObjs.ContainsKey(serviceName) && SingleObjs.ContainsKey(serviceName))
            throw new ServiceExistedException();
        if (ScopedObjs.ContainsKey(serviceName))
            instance = ScopedObjs[serviceName].Invoke();
        else if (SingleObjs.ContainsKey(serviceName))
            instance = SingleObjs[serviceName];
        if (instance != null)
        {
            IEnumerable<FieldInfo> fields = type
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<ServiceInjection>() != null);
            foreach (FieldInfo field in fields)
            {
                ServiceInjection fieldAttribute = field.GetCustomAttribute<ServiceInjection>();
                field.SetValue(instance, Get(field.FieldType, fieldAttribute.Name));
            }

            IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Instance |
                                                                      BindingFlags.NonPublic 
                                                                      | BindingFlags.Public)
                .Where(p=>p.GetCustomAttribute<ServiceInjection>() != null);
            foreach (var property in properties)
            {
                ServiceInjection fieldAttribute = property.GetCustomAttribute<ServiceInjection>();
                property.SetValue(instance, Get(property.PropertyType, fieldAttribute.Name));
            }
        }
        if (type.IsAssignableTo(typeof(IDisposable)) && !releasingObjs.Contains((IDisposable)instance))
            releasingObjs.Add((IDisposable)instance);
        
        return instance;
    }

    public  T? Get<T>(string name = "")
    {
        Type type = typeof(T);
        T? instance = (T)Get(type, name);
        if (instance == null)
            return default;
        /*if (type.IsAssignableTo(typeof(IDisposable)) && !releasingObjs.Contains((IDisposable)instance))
            releasingObjs.Add((IDisposable)instance);*/
        return instance;
    }

    public void AddService<T>(string name = "")
    {
        var type = typeof(T);
        scopedObjs[string.IsNullOrEmpty(name) ? type.Name : name] = () => Get(type, name);
    }

    public void AddService<I, T>(string name = "") where T : class where I : class
    {
        Type interfaceType = typeof(I);
        if (!interfaceType.IsInterface && !interfaceType.IsAbstract)
            throw new NotAbstractException();
        Type type = typeof(T);
        if (interfaceType.IsAssignableFrom(type))
            scopedObjs[string.IsNullOrEmpty(name) ? interfaceType.Name : name] = () => Activator.CreateInstance(type);
        else
            throw new NotImplTypeException();
    }

    public void AddService<I, T>(Func<IServiceProvider,T> func, string name = "") where T : class where I : class
    {
        Type interfaceType = typeof(I);
        if (!interfaceType.IsInterface && !interfaceType.IsAbstract)
            throw new NotAbstractException();
        Type type = typeof(T);
        if (interfaceType.IsAssignableFrom(type))
        {
            if (!string.IsNullOrEmpty(name))
                scopedObjs[name] = () => func(this);
            else
                scopedObjs[interfaceType.Name] = () => func(this);
        }
        else
            throw new NotImplTypeException();
    }
    

    public void AddService(Type type, Func<IServiceProvider,object> instanceFunc, string name = "")
    {
        if (string.IsNullOrEmpty(name))
            scopedObjs[type.Name] =() => instanceFunc(this);
        else
            scopedObjs[name] = () => instanceFunc(this);
    }

    public void AddService<T>(Func<IServiceProvider,T> func,string name = "") where T : class
    {
        if(string.IsNullOrEmpty(name))
            scopedObjs[typeof(T).Name] =() => func(this);
        else scopedObjs[name] = () => func(this);
    }

    public void AddSingle<T>(Func<IServiceProvider,T> instanceFunc, string name = "")
    {
        Type type = typeof(T);
        singleObjs[string.IsNullOrEmpty(name) ? type.Name : name] = instanceFunc(this);
    }

    public void AddSingle<T>(string name="")
    {
        if(string.IsNullOrEmpty(name))
            singleObjs[typeof(T).Name] = Activator.CreateInstance<T>();
        else
            singleObjs[name] = Activator.CreateInstance<T>();
    }

    public void AddSingle(Type type, Func<IServiceProvider,object> instanceFunc, string name = "")
    {
        singleObjs[string.IsNullOrEmpty(name) ? type.Name : name] = instanceFunc(this);
    }

    public void AddSingle<I, T>(string name = "") where T : class where I : class
    {
        Type interfaceType = typeof(I);
        if (!interfaceType.IsInterface && !interfaceType.IsAbstract)
            throw new NotAbstractException();
        Type type = typeof(T);
        if (interfaceType.IsAssignableFrom(type))
        {
            singleObjs[string.IsNullOrEmpty(name) ? interfaceType.Name : name] = Activator.CreateInstance(type);
        }
        else
            throw new NotImplTypeException();
    }

    public void AddSingle<I, T>(Func<IServiceProvider,T> instanceFunc, string name = "") where T : class where I : class
    {
        Type interfaceType = typeof(I);
        if (!interfaceType.IsInterface && !interfaceType.IsAbstract)
            throw new NotAbstractException();
        Type type = typeof(T);
        if (interfaceType.IsAssignableFrom(type))
        {
            if (name != "")
                singleObjs[name] = instanceFunc(this);
            else
                singleObjs[interfaceType.Name] = instanceFunc(this);
        }
        else
            throw new NotImplTypeException();
    }

    public bool HasInjected(Type type, string name)
    {
       return ScopedObjs.Any(p=>p.Value.GetType() == type && p.Key == name)
           || SingleObjs.Any(p=>p.Value.GetType() == type && p.Key == name);
    }

    public bool HasInjected<T>(string name)
    {
        return HasInjected(typeof(T), name);
    }

    public void ReleasingObjs()
    {
        foreach (IDisposable disposable in releasingObjs)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                continue;
            }
        }
    }

    public IEnumerable<Type> GetTypesWithBase(Type baseType)
    {
        var singleTypes = SingleObjs
            .Where(p => p.Value.GetType().IsAssignableTo(baseType))
            .Select(p => p.Value.GetType());
        var scopeTypes = ScopedObjs
            .Where(p=>p.Value.Method.ReturnType.IsAssignableTo(baseType))
            .Select(p => p.Value.Method.ReturnType);
        
        return singleTypes.Union(scopeTypes);
    }
}
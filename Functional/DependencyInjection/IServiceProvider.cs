namespace KWeb.DependencyInjection;

public interface IServiceProvider
{
    public object? Get(Type type,string name="");
    public T? Get<T>(string name="");
    public void AddService<T>(string name = "");
    public void AddService<I, T>(string name = "") where T : class where I : class;
    public void AddService<I, T>(Func<IServiceProvider,T> func, string name = "") where T : class where I : class;
    public void AddService(Type type, Func<IServiceProvider, object> instanceFunc, string name = "");
    public void AddService<T>(Func<IServiceProvider, T> func,string name = "") where T : class;
    public void AddSingle<T>(Func<IServiceProvider, T> instanceFunc, string name = "");
    public void AddSingle<T>(string name = "");
    public void AddSingle(Type type, Func<IServiceProvider, object> instanceFunc, string name = "");
    public void AddSingle<I, T>(string name = "") where T : class where I : class;

    public void AddSingle<I, T>(Func<IServiceProvider, T> instanceFunc, string name = "")
        where T : class where I : class;

    public bool HasInjected(Type type, string name);
    public bool HasInjected<T>(string name);
    public void ReleasingObjs();

    public IEnumerable<Type> GetTypesWithBase(Type baseType);

}
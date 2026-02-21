namespace KWeb.DependencyInjection;

public enum InjectionType
{
    None, Single = 1,Scoped = 2
}
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class
|AttributeTargets.Property)]
public class ServiceInjection : Attribute
{
    public string Name { get; set; }
    public InjectionType Type { get; set; }
    public ServiceInjection(string name = "", InjectionType type = InjectionType.Scoped)
    {
        Name = name;
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ConfigAttribute : ServiceInjection
{
    public string Key { get; set; }
}

public class ServiceExistedException : Exception
{
    public ServiceExistedException() : base("该服务已被注册了！") { }
}

public class NotImplementionException : Exception
{
    public NotImplementionException() : base("注入的实际类型不是注入接口/抽象类类型的实现类") { }
}

public class NotAbstractException : Exception
{
    public NotAbstractException() : base("注入的前置类型必须是接口或者抽象类") { }
}

public class NotImplTypeException : Exception
{
    public NotImplTypeException() : base("注入的后缀类非前置类的子类或实现类") { }
}
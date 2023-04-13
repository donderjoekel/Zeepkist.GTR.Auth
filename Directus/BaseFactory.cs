using System.Dynamic;

namespace TNRD.Zeepkist.GTR.Auth.Directus;

public class BaseFactory<TFactory>
    where TFactory : BaseFactory<TFactory>, new()
{
    protected dynamic model;

    protected BaseFactory()
    {
        model = new ExpandoObject();
    }

    public static TFactory New()
    {
        return new TFactory();
    }

    public TFactory WithId(int id)
    {
        model.id = id;
        return (TFactory)this;
    }

    public dynamic Build()
    {
        return model;
    }
}

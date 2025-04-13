using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.DI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScopedServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SingletonServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TransientServiceAttribute : Attribute { }

}

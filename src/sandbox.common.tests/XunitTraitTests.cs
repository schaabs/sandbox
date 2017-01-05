using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace sandbox.common.tests
{
    class XunitTraitTests
    {
        [Fact]
        [Trait("name_A", "value_A")]
        [Trait("name_B", "value_B")]
        public static void GetTraits()
        {
            foreach(var attrData in typeof(XunitTraitTests).GetMethod("GetTraits").GetCustomAttributesData())
            {
                if (attrData.AttributeType == typeof(TraitAttribute))
                {
                    foreach(var narg in attrData.NamedArguments)
                    {
                        Console.WriteLine(narg.MemberName, narg.TypedValue.Value);
                    }

                    foreach(var carg in attrData.ConstructorArguments)
                    {
                        Console.WriteLine(carg.Value);
                    }
                }
            }
        }
    }
}

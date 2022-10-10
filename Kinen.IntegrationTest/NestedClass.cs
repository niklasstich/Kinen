using Kinen.Generator;

namespace Kinen.IntegrationTest
{
    namespace Nested
    {
        namespace NestedAgain
        {
            public partial class ParentClass
            {
                [Memento]
                public partial class NestedClass
                {
                    public string Name { get; set; }
                    private int Number { get; set; }
                }
            }
        }
    }
}

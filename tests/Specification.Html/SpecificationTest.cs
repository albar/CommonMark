using System.Collections.Generic;
using Bunnypro.CommonMark.Html;
using Xunit;

namespace Bunnypro.CommonMark.Specification.Html.Test
{
    public class SpecificationTest
    {
        [Theory]
        [MemberData(nameof(Specifications))]
        public void Specification_Test(Specification specification)
        {
            var cmark = new Core.CommonMark();
            var document = cmark.Parse(specification.Markdown);
            Assert.Equal(specification.Html, document.ToHtml());
        }

        public static IEnumerable<object[]> Specifications()
        {
            var enumerator = new SpecificationEnumerator();
            while (enumerator.MoveNextSpecification())
                yield return new object[] { enumerator.GetCurrentSpecification() };
        }
    }
}

namespace Cloudy_Canvas.Tests.Helpers
{
    using System.Linq;
    using Cloudy_Canvas.Helpers;
    using Xunit;

    public class QueryHelperTests
    {
        [Theory]
        [InlineData("twilight sparkle", "(twilight sparkle), safe")]
        [InlineData("*", "(*), safe")]
        [InlineData("a || b", "(a || b), safe")]
        [InlineData("(a || b), c", "((a || b), c), safe")]
        [InlineData("id:123", "(id:123), safe")]
        public void SafeModeWrapsTheWholeQuery(string query, string expected)
        {
            Assert.Equal(expected, QueryHelper.ApplySafeMode(query, true));
        }

        [Fact]
        public void QueryIsUntouchedWhenSafeModeIsOff()
        {
            Assert.Equal("a || b", QueryHelper.ApplySafeMode("a || b", false));
        }

        [Theory]
        [InlineData("x) || (y")]
        [InlineData("x)) || ((y")]
        [InlineData(") || (")]
        [InlineData("(x")]
        public void UnbalancedParenthesesCannotCloseTheSafeModeGroup(string query)
        {
            var result = QueryHelper.ApplySafeMode(query, true);

            // The only parentheses left are the wrapper's own, so nothing can escape the group.
            Assert.StartsWith("(", result);
            Assert.EndsWith("), safe", result);
            Assert.Equal(1, result.Count(c => c == '('));
            Assert.Equal(1, result.Count(c => c == ')'));
        }

        [Theory]
        [InlineData("a, b", true)]
        [InlineData("(a || b), c", true)]
        [InlineData("((a))", true)]
        [InlineData("\"a (b\"", true)] // parenthesis inside quotes doesn't count
        [InlineData("\"a \\\" (b\"", true)] // escaped quote stays inside the quoted text
        [InlineData("(a", false)]
        [InlineData("a)", false)]
        [InlineData(")a(", false)]
        [InlineData("\"a", false)]
        public void BalancedParenthesesCheck(string query, bool expected)
        {
            Assert.Equal(expected, QueryHelper.HasBalancedParentheses(query));
        }
    }
}

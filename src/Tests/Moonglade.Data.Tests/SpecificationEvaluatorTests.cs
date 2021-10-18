using Moonglade.Data.Infrastructure;
using NUnit.Framework;

namespace Moonglade.Data.Tests
{
    [TestFixture]
    public class SpecificationEvaluatorTests
    {
        [Test]
        public void GetQuery_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var inputQuery = new List<Fubao>
            {
                new() { Id = 1, Name = "Fubao1" },
                new() { Id = 2, Name = "Fubao2" },
                new() { Id = 3, Name = "996-1" },
                new() { Id = 4, Name = "996-2" }
            }.AsQueryable();
            ISpecification<Fubao> specification = new FubaoSpec();

            // Act
            var result = SpecificationEvaluator<Fubao>.GetQuery(
                inputQuery,
                specification).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
        }
    }

    internal class Fubao
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal sealed class FubaoSpec : BaseSpecification<Fubao>
    {
        public FubaoSpec() : base(p => p.Id > 1)
        {
            AddCriteria(p => p.Name.StartsWith("996"));
            ApplyOrderByDescending(p => p.Name);
            ApplyPaging(0, 996);
        }
    }
}

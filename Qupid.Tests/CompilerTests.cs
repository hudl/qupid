using System.Text.RegularExpressions;
using Qupid.Compile;
using Xunit;

namespace Qupid.Tests
{
    public class CompilerTests
    {
        [Fact]
        public void SelectWithWhereGroupAndWithClauses_Success()
        {
            const string query = @"select Foo.Name, Foo.COUNT
                                  from Foo
                                  where Foo.FooId <> '521d67620e281a036092ea7e'
                                  group by Foo.Name";
            var compiler = new Compiler(new TestCollectionFinder());
            var compiledQuery = compiler.Compile(query);
            var cleanedStatement = Regex.Replace(compiledQuery.GetMongoQuery(), "\\s+", " ").Trim();

            Assert.False(compiledQuery.HasErrors());
            Assert.Equal("{ aggregate:'foo', pipeline: [ {$match: {'_id':{$ne:ObjectId('521d67620e281a036092ea7e')}}}, { $group: { _id:'$n', Foo_count: {$sum:1}} }, ] }", cleanedStatement);
        }

        [Fact]
        public void Select_WhereBsonObjectIdEquals_Success()
        {
            var compiler = new Compiler(new TestCollectionFinder());

            const string query = @"select Foo.Name, Foo.COUNT
                                  from Foo
                                  where Foo.FooId = '521d67620e281a036092ea7e'
                                  group by Foo.Name";
            var compiledQuery = compiler.Compile(query);
            var cleanedStatement = Regex.Replace(compiledQuery.GetMongoQuery(), "\\s+", " ").Trim();

            Assert.False(compiledQuery.HasErrors());
            Assert.Equal("{ aggregate:'foo', pipeline: [ {$match: {'_id':ObjectId('521d67620e281a036092ea7e')}}, { $group: { _id:'$n', Foo_count: {$sum:1}} }, ] }", cleanedStatement);
        }

        [Fact]
        public void Select_CaseInsensitive_SameResult()
        {
            var compiler = new Compiler(new TestCollectionFinder());

            const string query1 = @"SELECT Foo.FooId
                                    FROM Foo
                                    WHERE Foo.FooId = 123";
            var result1 = compiler.Compile(query1).GetMongoQuery();
            const string query2 = @"select Foo.FooId
                                    from Foo
                                    where Foo.FooId = 123";
            var result2 = compiler.Compile(query2).GetMongoQuery();

            Assert.Equal(result1, result2);
        }
        
        [Fact]
        public void Select_WhereByDate_SingleQuotes_Success()
        {
            const string query = @"SELECT Foo.FooId
                                    FROM Foo
                                    WHERE Foo.DateCreated > '2013-04-11'";
            var compiler = new Compiler(new TestCollectionFinder());
            var compiledQuery = compiler.Compile(query);
            var cleanedStatement = Regex.Replace(compiledQuery.GetMongoQuery(), "\\s+", " ").Trim();

            Assert.False(compiledQuery.HasErrors());
            Assert.Equal("{ aggregate:'foo', pipeline: [ {$match: {'dc':{$gt:new Date('2013-04-11')}}}, {$project:{ '_id':1 }}, ] }", cleanedStatement);
        }

        [Fact]
        public void Select_WhereByDate_DoubleQuotes_Success()
        {
            const string query = @"SELECT Foo.FooId
                                    FROM Foo
                                    WHERE Foo.DateCreated > ""2013-04-11""";
            var compiler = new Compiler(new TestCollectionFinder());
            var compiledQuery = compiler.Compile(query);
            var cleanedStatement = Regex.Replace(compiledQuery.GetMongoQuery(), "\\s+", " ").Trim();

            Assert.False(compiledQuery.HasErrors());
            Assert.Equal("{ aggregate:'foo', pipeline: [ {$match: {'dc':{$gt:new Date(\"2013-04-11\")}}}, {$project:{ '_id':1 }}, ] }", cleanedStatement);
        }

        [Fact]
        public void SelectStar_WhereByDate_Success()
        {
            const string query = @"SELECT Foo.*
                                    FROM Foo
                                    WHERE Foo.DateCreated > '2013-04-11'";
            var compiler = new Compiler(new TestCollectionFinder());
            var compiledQuery = compiler.Compile(query);
            var cleanedStatement = Regex.Replace(compiledQuery.GetMongoQuery(), "\\s+", " ").Trim();

            Assert.False(compiledQuery.HasErrors());
            Assert.Equal("{ aggregate:'foo', pipeline: [ {$match: {'dc':{$gt:new Date('2013-04-11')}}}, {$project:{ '_id':1, 'n':1, 'dc':1 }}, ] }", cleanedStatement);
        }

        
    }
}

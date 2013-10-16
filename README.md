qupid
=====

C# API for running sql-like queries against MongoDB. Qupid supports a sub-set of SQL and offers a few additional operators. It supports introspecting
classes that are marked up with [BsonElement] and [BsonId] attributes to map between the "long name" (C# property names) and "short name" (property
name in the MongoDB collection). We typically use one or two character names within our collections to save space. Qupid makes it easier to query
because we can reference the "long name" in our queries.

Written by [Brian Kaiser](https://github.com/Briankaiser) and [Jon Dokulil](https://github.com/agilejon)

Qupid is really two pieces:

1. [```Qupid.Compile.Compiler```](https://github.com/hudl/qupid/blob/master/Qupid/Compile/Compiler.cs) - takes a Qupid query and converts it to a MongoDB aggregation statement
2. [```Qupid.Execution.QueryExecutor```](https://github.com/hudl/qupid/blob/master/Qupid/Execution/QueryExecutor.cs) - takes a compiled Qupid query and executes it

The query grammar:
```
SELECT select_clause
FROM collectionName
(WHERE where_clause)?
(UNWIND property_reference)?
(GROUP BY property_reference)?
(HAVING property_reference comparator constant)?

select_clause:
	property_reference (, property_reference)*

where_clause:
	where_phrase ((AND|OR) where_phrase)*

where_phrase:
	property_reference comparator constant

property_reference:
	collectionName(.(propertyName|*))+

comparator:
	<> | = | > | >= | < | <=

constant:
	string_constant | [1-9][0-9]*

string_constant:
	(\"|\') .* (\"|\')
```

You can see some example queries in https://github.com/hudl/qupid/blob/master/Qupid.Tests/CompilerTests.cs


Modifying the Grammar:
====
1. Make changes to [QuerySyntax.g](https://github.com/hudl/qupid/blob/master/Qupid/AutoGen/QuerySyntax.g) (an Antlr 3.5 grammar file)
2. Pop open a cmd window and navigate to {repo}/AutoGen
3. Run generate.bat

When you make changes to the grammar, you will probably want to also modify [```QueryAnalyzer```](https://github.com/hudl/qupid/blob/master/Qupid/Compile/QueryAnalyzer.cs) to add any errors/warnings and do post-processing on the parsed AST.

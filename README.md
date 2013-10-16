qupid
=====

C# API for running sql-like queries against MongoDB. Qupid supports a sub-set of SQL and offers a few additional operators. It supports introspecting
classes that are marked up with [BsonElement] and [BsonId] attributes to map between the "long name" (C# property names) and "short name" (property
name in the MongoDB collection). We typically use one or two character names within our collections to save space. Qupid makes it easier to query
because we can reference the "long name" in our queries.

Qupid is really two pieces:
1. ```Qupid.Compile.Compiler``` - takes a Qupid query and converts it to a MongoDB aggregation statement
2. ```Qupid.Execution.QueryExecutor``` - takes a compiled Qupid query and executes it

The grammar for running queries:
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

Some example queries:


Modifying the Grammar:
====
1. Pop open a cmd window and navigate to {repo}/AutoGen
2. Run generate.bat

That will recreate the ```QuerySyntaxLexer.cs``` and ```QuerySyntaxParser.cs``` files.
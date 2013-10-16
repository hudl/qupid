/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

grammar QuerySyntax;
options {
    language = CSharp3;
    output = AST;
}

tokens {
	STAR = '*';
	DOT = '.';
	COMMA = ',';
}

@namespace { Qupid.AutoGen }
@lexer::namespace { Qupid.AutoGen }

@header {
	using System;
	using System.Text;
	using Qupid.AST;
}

@members {
	public QupidQuery Parse() {
		var result = main();
		return result.q;
	}
}


main returns[QupidQuery q]:
    qq = select_statement EOF
    { $q = $qq.q; }
    ;

select_statement returns[QupidQuery q]:
    		SELECT 
    pl =	column_list 
    f =		from_clause 
    wr =	where_clause?
	uw =	unwind_clause?
    g =		group_by_clause?
	h =		having_clause?
	with =	with_clause?
    {
    	$q = new QupidQuery($pl.pl, $f.s, $wr.wc, $uw.uw, $g.g, $h.h, $with.w);
        $q.ErrorManager = _errorManager;
    }
    ;
    
column_list returns[PropertyList pl]:
    { $pl = new PropertyList(); }
    scp = select_clause { $pl.Add($scp.pr); }
    (',' scp = select_clause { $pl.Add($scp.pr); })*
    ;

from_clause returns[string s]:
    FROM col = ID
	{ $s = $col.text; }
	;

group_by_clause returns[GroupByClause g]
	:	GROUP 
		BY
	pr =	property_reference
	{ $g = new GroupByClause($pr.pr); }
	;

having_clause returns[HavingClause h]:
                HAVING 
    lhsRef =    property_reference 
    comp =      comparison 
    rhsRef =    constant_or_reference
    { 
    	var literal = $rhsRef.strLiteral == null ? (object)$rhsRef.numLiteral : (object)$rhsRef.strLiteral;
    	$h = new HavingClause($lhsRef.pr, GetComparison($comp.start), literal);
    }
    ;

with_clause returns[WithClause w]:
				WITH
	joinOn =	ID
				ON
	pr =		property_reference
	{
		$w = new WithClause($joinOn.text, $pr.pr);
	}
	;

unwind_clause returns[UnwindClause uw]:
				UNWIND
	prop =		property_reference
	{
		$uw = new UnwindClause($prop.pr);
	}
	;

where_clause returns[List<WhereClause> wc]:
    { $wc = new List<WhereClause>(); }
                WHERE 
    lhsRef =    property_reference 
    comp =      comparison 
    rhsRef =    constant_or_reference
    { 
    	var literal = $rhsRef.strLiteral == null ? (object)$rhsRef.numLiteral : (object)$rhsRef.strLiteral;
    	$wc.Add(new WhereClause($lhsRef.pr, GetComparison($comp.start), literal));
    }
                (a = additional_where_phrase { $wc.Add($a.c); } )*
    ;

additional_where_phrase returns[WhereClause c]:
    op =        boolean_operator 
    lhsRef =    property_reference 
    comp =      comparison 
    rhsRef =    constant_or_reference
    { 
        var literal = $rhsRef.strLiteral == null ? (object)$rhsRef.numLiteral : (object)$rhsRef.strLiteral;
        $c = new WhereClause(GetBoolOp($op.start.Type), $lhsRef.pr, GetComparison($comp.start), literal);
    }
    ;

boolean_operator:
    AND |
    OR;

constant_or_reference returns[string strLiteral, long numLiteral]
    :
    literal =   STRING_LITERAL { $strLiteral = $literal.text; }
                |
    num =       NUMBER_LITERAL { $numLiteral = long.Parse($num.text); }
    ;

select_clause returns[PropertyReference pr]:
    p = property_reference { $pr = $p.pr; }
    ;

property_reference returns[PropertyReference pr]:
    { StringBuilder sb = new StringBuilder(); }
    col =       ID 
                ( '.'
    		      ( part = ID { if (sb.Length > 0) { sb.Append("."); } sb.Append($part.text); } 
                  | STAR { sb.Append("*"); }
                  )
                )+
    { 
        $pr = PropertyReference.GetReference($col.text, sb.ToString(), $col.Line, $col.CharPositionInLine);
    }
    ;
	catch [EarlyExitException eee] 
	{
		_errorManager.AddError("I don't know what to make of '" + $col.text + "', properties need be of the form collection.property");
	}

comparison:
    NOT_EQUALS |
    EQUALS |
	GREATER_THAN |
	LESS_THAN |
	GREATER_THAN_EQUAL |
	LESS_THAN_EQUAL;

HAVING : ('H'|'h')('A'|'a')('V'|'v')('I'|'i')('N'|'n')('G'|'g');
UNWIND : ('U'|'u')('N'|'n')('W'|'w')('I'|'i')('N'|'n')('D'|'d');
GROUP : ('G'|'g')('R'|'r')('O'|'o')('U'|'u')('P'|'p');
BY : ('B'|'b')('Y'|'y');
JOIN : ('J'|'j')('O'|'o')('I'|'i')('N'|'n');
FROM : ('F'|'f')('R'|'r')('O'|'o')('M'|'m');
WHERE : ('W'|'w')('H'|'h')('E'|'e')('R'|'r')('E'|'e');
WITH : ('W'|'w')('I'|'i')('T'|'t')('H'|'h');
ON : ('O'|'o')('N'|'n');
SELECT : ('S'|'s')('E'|'e')('L'|'l')('E'|'e')('C'|'c')('T'|'t');
EQUALS : '=';
NOT_EQUALS : '<>';
GREATER_THAN : '>';
GREATER_THAN_EQUAL : '>=';
LESS_THAN : '<';
LESS_THAN_EQUAL : '<=';
AND : 'AND' | 'and';
OR : 'OR' | 'or';
WS : (' '|'\r'|'\t'|'\n')+ {$channel=Hidden;};
ID : ('_'|'a'..'z'|'A'..'Z') ('_'|'a'..'z'|'A'..'Z'|'0'..'9')*;
NUMBER_LITERAL : ('0'..'9')+;
STRING_LITERAL : QUOTE (options {greedy=false;} : .)* QUOTE;
QUOTE : '\"' | '\'';
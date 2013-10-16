using System;
using Antlr.Runtime;
using Qupid.AST;

namespace Qupid.AutoGen
{
    public partial class QuerySyntaxParser
    {
        private readonly ErrorManager _errorManager;

        public QuerySyntaxParser(ITokenStream stream, ErrorManager em)
            : this(stream)
        {
            _errorManager = em;
        }

        public static QupidQuery ParseString(string query, ErrorManager errorManager)
        {
            var lexer = new QuerySyntaxLexer(new ANTLRStringStream(query));
            var parser = new QuerySyntaxParser(new CommonTokenStream {TokenSource = lexer}, errorManager);
            var ast = parser.Parse();
            return ast;
        }

        protected Comparison GetComparison(IToken token)
        {
            if (token == null)
            {
                throw new RecognitionException("Not sure what to make of this. I was expecting >, <, >=, <=, =, or <> but didn't find it");
            }

            switch (token.Type)
            {
                case EQUALS:
                    return Comparison.Equals;

                case NOT_EQUALS:
                    return Comparison.NotEquals;

                case GREATER_THAN:
                    return Comparison.GreaterThan;

                case LESS_THAN:
                    return Comparison.LessThan;

                case GREATER_THAN_EQUAL:
                    return Comparison.GreaterThanEquals;

                case LESS_THAN_EQUAL:
                    return Comparison.LessThanEquals;

                default:
                    throw new RecognitionException("I'm confused. I was expecting >, <, >=, <=, =, or <> but instead found '" + token.Text + "'")
                        {
                            Line = token.Line,
                            CharPositionInLine = token.CharPositionInLine,
                        };
            }
        }

        protected BooleanOperand GetBoolOp(int boolOp)
        {
            switch (boolOp)
            {
                case AND:
                    return BooleanOperand.And;

                case OR:
                    return BooleanOperand.Or;

                default:
                    throw new RecognitionException("Unsupported boolean operator " + boolOp);
            }
        }

        protected void CheckPropertyValue(string prop)
        {
            
        }

        public override void ReportError(RecognitionException e)
        {
            base.ReportError(e);

            // try to apply some intelligence and help out with common mistakes
            var mmte = e as MismatchedTokenException;
            if (mmte != null)
            {
                switch (mmte.Expecting)
                {
                    case QuerySyntaxLexer.FROM:
                        AddError(e, "Found '" + e.Token.Text + "' but was expecting 'FROM', are you missing a comma?");
                        return;

                    case QuerySyntaxLexer.ID:
                        if (e.Token.Text.Equals("*", StringComparison.Ordinal))
                        {
                            AddError(e, "We don't yet support '*' queries. You'll need to list each property.");
                            return;
                        }
                        if (e.Token.Type == QuerySyntaxLexer.WITH)
                        {
                            AddError(e, "Found '" + e.Token.Text + "' but was expecting another property");
                            return;
                        }
                        break;
                }
            }

            if (e is NoViableAltException)
            {
                if (e.Token.Text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    _errorManager.AddError("'true' should be expressed as '1'", e.Token.Line, e.Token.CharPositionInLine);
                    return;
                }
                if (e.Token.Text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    _errorManager.AddError("'false' should be expressed as '0'", e.Token.Line, e.Token.CharPositionInLine);
                    return;
                }
            }

            if (e.Token != null)
            {
                if (e.Token.Text.Equals("where", StringComparison.OrdinalIgnoreCase))
                {
                    _errorManager.AddError("Unexpected 'where', it must come before 'unwind', 'group by', or 'with'",
                                           e.Token.Line, e.Token.CharPositionInLine);
                }
                else if (e.Token.Text.Equals("unwind", StringComparison.OrdinalIgnoreCase))
                {
                    _errorManager.AddError("Unexpected 'unwind', it must come before 'group by', or 'with'",
                                           e.Token.Line, e.Token.CharPositionInLine);
                }
                else if (e.Token.Text.Equals("group", StringComparison.OrdinalIgnoreCase))
                {
                    _errorManager.AddError("Unexpected 'group by', it must come before 'with'",
                                           e.Token.Line, e.Token.CharPositionInLine);
                }
                else if (e.Message.Equals("A recognition error occurred.", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Token.Text.Equals("NOT", StringComparison.OrdinalIgnoreCase) || e.Token.Text.Equals("IN", StringComparison.OrdinalIgnoreCase))
                    {
                        _errorManager.AddError("I don't understand '" + e.Token.Text + "' (comparisons that I understand are =, <>, >, >=, < and <=)", e.Line, e.CharPositionInLine);
                    }
                    else
                    {
                        // the generic parser error message isn't super helpful. Do our best to make it more helpful
                        _errorManager.AddError("I don't understand '" + e.Token.Text + "'", e.Line, e.CharPositionInLine);
                    }
                }
            }
            else
            {
                _errorManager.AddError(e.Message, e.Line, e.CharPositionInLine);
            }
        }

        private void AddError(RecognitionException e, string message)
        {
            _errorManager.AddError(message, e.Token.Line, e.Token.CharPositionInLine);
        }
    }
}

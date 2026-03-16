parser grammar RuleTemplateParser;
options { tokenVocab = RuleTemplateLexer; }

template
    : templatePart* EOF
    ;

templatePart
    : TEXT                                  #LiteralPart
    | ANY_OTHER_SLASH                       #LiteralSlashPart
    | ESCAPED_BRACE                         #EscapedBrace
    | OPEN_BRACE expression CLOSE_BRACE     #InterpolationPart
    ;

expression
    : accessor (NULL_COALESCE accessor)*    #NullCoalesceExpr
    | accessor                              #AccessorExpr
    ;

accessor
    : IDENTIFIER ( DOT IDENTIFIER | LBRACK INT_LITERAL RBRACK )*
    ;

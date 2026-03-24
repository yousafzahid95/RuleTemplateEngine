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
    : expression NULL_COALESCE expression          # NullCoalesceExpr
    | expression EQ expression                     # EqualityExpr
    | expression OR expression                     # LogicalOrExpr
    | expression QMARK expression COLON expression # TernaryExpr
    | accessor                                     # AccessorExpr
    | STRING_LITERAL                               # StringLiteralExpr
    ;

accessor
    : IDENTIFIER ( DOT IDENTIFIER | LBRACK INT_LITERAL RBRACK )*
    ;

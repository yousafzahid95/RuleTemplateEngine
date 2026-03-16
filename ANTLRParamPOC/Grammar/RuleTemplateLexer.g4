lexer grammar RuleTemplateLexer;

TEXT          : ~[{\\]+ ;
ESCAPED_BRACE : '\\{' ;
ANY_OTHER_SLASH : '\\' ;
OPEN_BRACE    : '{' -> pushMode(EXPR_MODE) ;

mode EXPR_MODE;
CLOSE_BRACE   : '}' -> popMode ;
NULL_COALESCE : '??' ;
DOT           : '.' ;
LBRACK        : '[' ;
RBRACK        : ']' ;
IDENTIFIER    : [a-zA-Z_][a-zA-Z_0-9]* ;
INT_LITERAL   : [0-9]+ ;
EXPR_WS       : [ \t]+ -> skip ;

grammar Grammar;

compileUnit : expression EOF;

expression :
      LPAREN expression RPAREN                                      #ParenthesizedExpr
    | operatorToken=(ADD | SUBTRACT) expression                     #UnaryExpr
    | expression operatorToken=(MULTIPLY | DIVIDE | DIV | MOD) expression  #MultiplicativeExpr
    | expression operatorToken=(ADD | SUBTRACT) expression               #AdditiveExpr
    | CELLREF                                                      #CellReferenceExpr
    | NUMBER                                                             #NumberExpr
;

/*
* Lexer Rules
*/
NUMBER : INT ('.' INT)?;
INT : ('0'..'9')+;
CELLREF : [A-Z]+ [0-9]+;  // Токен для посилань на клітинки, наприклад "A1", "B12"
DIV : 'div';
MOD : 'mod';
MULTIPLY : '*';
DIVIDE : '/';
SUBTRACT : '-';
ADD : '+';
LPAREN : '(';
RPAREN : ')';
WS : [ \t\r\n] -> channel(HIDDEN);

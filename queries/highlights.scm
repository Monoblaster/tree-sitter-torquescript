; highlights.scm
"*" @operator
"/" @operator
(binary_expression "%" @operator)
"+" @operator
"-" @operator
"++" @operator
"--" @operator
"<" @operator
">" @operator
"<=" @operator
">=" @operator
"==" @operator
"!=" @operator
"!" @operator
"&&" @operator
"||" @operator
"~" @operator
"&" @operator
"|" @operator
"^" @operator
"<<" @operator
">>" @operator
"@" @operator
"NL" @operator
"TAB" @operator
"SPC" @operator
"$=" @operator
"!$=" @operator
"=" @operator

(comment) @comment

(function_identifier class: (identifier) @type)
(function_identifier name: (identifier) @function)

[ "("
  ")"
  "{"
  "}"
  "]"
  "["
  ";"
] @punctuation.bracket

"," @punctuation.delimiter

(parenthesized_variables (variable) @variable.parameter)
(expression/variable) @variable
(assignment_expression (variable) @variable)
(variable "$" @operator) 
(variable "%" @operator)
(field name: (identifier) @variable.member)
(object_name) @type

(number) @number

(string) @string
(tagged_string) @string

(markup) @string.special
(tag) @string.special
(escape_sequence) @string.escape

(new_expression (identifier) @type)

[ (false)
 (true)
] @boolean

"function" @keyword.function

[ "while"
  "for"
] @keyword.repeat

[ "break"
  "continue"
  "return" 
]@keyword.return

[ "if"
  "else"
] @keyword.conditional

(conditional "?" @keyword.conditional.ternary)
(conditional ":" @keyword.conditional.ternary)

[ "or"
  "and"
] @keyword.operator

[ "case"
  "datablock"
  "default"
  "new"
  "package"
  "switch"
  "switch$"
] @keyword

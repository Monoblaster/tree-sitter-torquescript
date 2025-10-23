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

[ "("
  ")"
  "{"
  "}"
  "]"
  "["
  ";"
] @punctuation.bracket

"," @punctuation.delimiter

(function_identifier class: (identifier) @type)
(function_identifier class: (parent) @keyword)
(function_identifier name: (identifier) @function)

(parenthesized_variables (variable) @variable.parameter)
(expression/variable) @variable
(assignment_expression (variable) @variable)
(variable "$" @operator) 
(variable "%" @operator)
(field name: (identifier) @property)
(object_name) @string.special

(number) @number

(string) @string
(tagged_string) @string

(markup) @tag
(tag) @tag
(escape_sequence) @string.escape

(new_expression type: (identifier) @type.builtin)
(datablock type: (identifier) @type.builtin)
(datablock name: (identifier) @type.definition)
(datablock inherit: (identifier) @type)
(field_block name: (identifier) @variable.member)

[ (false)
 (true)
] @boolean

(field_block name: [(class) (superclass) (classname)] @variable.builtin)

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
  "switch"
  "switch$"
  "case"
  "default"
] @keyword.conditional

(conditional "?" @keyword.conditional.ternary)
(conditional ":" @keyword.conditional.ternary)

[ "or"
  "and"
] @keyword.operator

[ "datablock"
  "new"
] @keyword.type

"package" @keyword

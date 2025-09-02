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

(function_definition "function" @keyword.function)
(function_definition name: (name) @function)
(function_definition namespace: (name) @type)

[ "("
  ")"
  "{"
  "}"
] @punctuation.bracket

[ ";"
  "."
  ","
] @punctuation.delimiter

[ "["
  "]"
] @punctuation.special

(local_variable) @variable
(global_variable) @variable
(object_field name: (name) @property)

(number) @number

(string_literal) @string
(tagged_string_literal) @string

(markup) @tag
(escape_sequence) @tag

(function name: (name) @function.call)
(function namespace: (name) @type)

(method name: (name) @function.method.call)

(new name: (name) @type)

[ (boolean_false)
 (boolean_true)
] @boolean

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

/**
 * @file The language used in the Torque 3D Game Engine
 * @author Monoblaster
 * @license MIT
 */

/// <reference types="tree-sitter-cli/dsl" />
// @ts-check

const PREC = {
  OR: 1, // or
  AND: 2, // and
  NOT: 3,
  COMPARE: 4, // < > <= >= ~= ==
  STRING: 5,
  BIT_OR: 6, // |
  BIT_XOR: 7, // ~
  BIT_AND: 8, // &
  BIT_SHIFT: 9, // << >>
  BIT_COMP: 10,
  ADD: 12, // + -
  MULT: 13, // * / // %
  ASSIGN: 14,
};

module.exports = grammar({
  name: "torquescript",

  extras: ($) => [$.comment, /\s/],
  word: ($) => $.identifier,

  supertypes: ($) => [$.expression],

  rules: {
    source_file: ($) => repeat(choice($._statement, $._definition)),

    _statement_block: ($) => seq("{", repeat($._statement), "}"),

    field_block: ($) =>
      seq(
        "{",
        repeat(
          seq(
            field(
              "name",
              choice(
                alias(/class/i, $.class),
                alias(/superclass/i, $.superclass),
                alias(/classname/i, $.classname),
                $.identifier,
              ),
            ),
            optional($.array),
            "=",
            $.expression,
            ";",
          ),
        ),
        "}",
      ),

    _optional_bracket_block: ($) => choice($._statement, $._statement_block),

    _statement: ($) =>
      choice(
        $.case_statement,
        $.assignment_statement,
        $.continue_statement,
        $.break_statement,
        $.return_statement,
        $.switch_statement,
        $.switch$_statement,
        $.for_statement,
        $.while_statement,
        $.if_statement,
        $.new_statement,
        $.function_call_statement,
        $.method_call_statement,
      ),

    case_statement: ($) =>
      prec.right(
        seq(
          choice(seq("case", $.expression), "default"),
          ":",
          repeat($._statement),
        ),
      ),

    return_statement: ($) => seq("return", optional($.expression), ";"),

    continue_statement: ($) => seq("continue", ";"),

    break_statement: ($) => seq("break", ";"),

    parenthesized_expression: ($) => seq("(", optional($.expression), ")"),

    parenthesized_variables: ($) =>
      seq("(", repeat(seq($.variable, ",")), optional($.variable), ")"),

    parenthesized_expressions: ($) =>
      seq("(", repeat(seq($.expression, ",")), optional($.expression), ")"),

    switch_statement: ($) =>
      seq("switch", $.parenthesized_expression, optional($._statement_block)),

    switch$_statement: ($) =>
      seq("switch$", $.parenthesized_expression, optional($._statement_block)),

    function_call_statement: ($) => seq($.function_call_expression, ";"),

    method_call_statement: ($) => seq($.method_call_expression, ";"),

    for_statement: ($) =>
      prec.right(
        seq(
          "for",
          "(",
          $.expression,
          ";",
          $.expression,
          ";",
          $.expression,
          ")",
          optional($._optional_bracket_block),
        ),
      ),

    while_statement: ($) =>
      prec.right(
        seq(
          "while",
          $.parenthesized_expression,
          optional($._optional_bracket_block),
        ),
      ),

    if_statement: ($) =>
      prec.right(
        seq(
          "if",
          $.parenthesized_expression,
          optional($._optional_bracket_block),
          optional($.else_statement),
        ),
      ),

    else_statement: ($) =>
      prec.right(seq("else", optional($._optional_bracket_block))),

    new_statement: ($) => seq($.new_expression, ";"),

    assignment_statement: ($) => seq($.assignment_expression, ";"),

    _definition: ($) => choice($.function, $.datablock, $.package),

    function: ($) =>
      seq(
        "function",
        $.function_identifier,
        $.parenthesized_variables,
        optional($._statement_block),
      ),

    datablock: ($) =>
      seq(
        "datablock",
        field("type", $.identifier),
        "(",
        field("name", $.identifier),
        optional(seq(":", field("inherit", $.identifier))),
        ")",
        optional($.field_block),
        ";",
      ),

    package: ($) =>
      seq(
        "package",
        field("name", alias($.identifier, $.object_name)),
        optional(seq("{", repeat($.function), "}", ";")),
      ),

    expression: ($) =>
      choice(
        $.true,
        $.false,
        $.number,
        $.string,
        $.tagged_string,
        $.array,
        $.variable,
        $.function_call_expression,
        $.field,
        $.method_call_expression,
        $.new_expression,
        $.binary_expression,
        $.unary_expression,
        $.assignment_expression,
        $.grouping,
        $.conditional,
        $.variable,
        alias($.identifier, $.object_name),
      ),

    true: ($) => /true/i,

    false: ($) => /false/i,

    binary_expression: ($) =>
      choice(
        ...[
          ["@", PREC.STRING],
          ["TAB", PREC.STRING],
          ["SPC", PREC.STRING],
          ["NL", PREC.STRING],
          ["*", PREC.MULT],
          ["/", PREC.MULT],
          ["%", PREC.MULT],
          ["+", PREC.ADD],
          ["-", PREC.ADD],
          ["<", PREC.COMPARE],
          [">", PREC.COMPARE],
          ["<=", PREC.COMPARE],
          [">=", PREC.COMPARE],
          ["==", PREC.COMPARE],
          ["!=", PREC.COMPARE],
          ["$=", PREC.COMPARE],
          ["!$=", PREC.COMPARE],
          ["&&", PREC.AND],
          ["and", PREC.AND],
          ["||", PREC.OR],
          ["or", PREC.OR],
          ["&", PREC.BIT_AND],
          ["|", PREC.BIT_OR],
          ["^", PREC.BIT_XOR],
          ["<<", PREC.BIT_SHIFT],
          [">>", PREC.BIT_SHIFT],
        ].map(([operator, precedence]) =>
          prec.left(
            precedence,
            seq(
              field("left", $.expression),
              operator,
              field("right", $.expression),
            ),
          ),
        ),
      ),

    unary_expression: ($) =>
      choice(
        ...[
          ["~", PREC.BIT_COMP],
          ["!", PREC.NOT],
          ["-", PREC.ADD],
        ].map(([operator, precedence]) =>
          prec.right(precedence, seq(operator, field("right", $.expression))),
        ),
      ),

    assignment_expression: ($) =>
      prec.right(
        seq(
          choice($.variable, $.field),
          choice(
            seq(
              choice(
                "=",
                "-=",
                "*=",
                "/=",
                "%=",
                "+=",
                "&=",
                "|=",
                "^=",
                "<<=",
                ">>=",
              ),
              $.expression,
            ),
            choice("++", "--"),
          ),
        ),
      ),

    method_call_expression: ($) =>
      seq(
        $.expression,
        ".",
        $.function_identifier,
        $.parenthesized_expressions,
      ),

    function_call_expression: ($) =>
      seq($.function_identifier, $.parenthesized_expressions),

    array: ($) =>
      seq("[", repeat(seq($.expression, ",")), optional($.expression), "]"),

    field: ($) =>
      prec(
        1,
        seq($.expression, ".", field("name", $.identifier), optional($.array)),
      ),

    variable: ($) =>
      seq(
        choice("$", "%"),
        field("name", $.variable_identifier),
        optional($.array),
      ),

    new_expression: ($) =>
      seq(
        "new",
        field("type", $.identifier),
        "(",
        optional($.expression),
        ")",
        optional($.field_block),
      ),

    number: ($) =>
      token(
        choice(
          /\d+/,
          /\d+\.\d+/,
          /\d+\.\d+E[+-]\d+/,
          /0x[0-9&&a-f]+/,
          /inf/i,
          /nan/i,
          /-nan/i,
        ),
      ),

    grouping: ($) => seq("(", $.expression, ")"),

    conditional: ($) =>
      prec.left(
        seq(
          field("test", $.expression),
          "?",
          field("true", $.expression),
          ":",
          field("false", $.expression),
        ),
      ),

    string: ($) =>
      seq(
        '"',
        repeat(
          choice(
            alias(
              token.immediate(
                choice(
                  "%",
                  seq("%", /[^0-9\\"\n<>%][^\\"\n<>%]+/),
                  seq("<", /[^\\"\n<>%]*/),
                  seq(">", /[^\\"\n<>%]*/),
                  /[^\\"\n<>%]+/,
                ),
              ),
              $.content,
            ),
            $.markup,
            $.escape_sequence,
            $.tag,
          ),
        ),
        '"',
      ),

    tagged_string: ($) =>
      seq(
        "'",
        repeat(
          choice(
            alias(
              token.immediate(
                choice(
                  "%",
                  seq("%", /[^0-9\\'\n<>%][^\\"\n<>%]+/),
                  seq("<", /[^\\'\n<>%]*/),
                  seq(">", /[^\\'\n<>%]*/),
                  /[^\\'\n<>%]+/,
                ),
              ),
              $.content,
            ),
            $.markup,
            $.escape_sequence,
            $.tag,
          ),
        ),
        "'",
      ),

    escape_sequence: ($) =>
      token(prec(1, seq("\\", choice(/[^cx]/, /c[rpo0-9]/, /x[a-z]{2}/)))),

    markup: ($) => token(/<[^<>"']*>/),

    tag: ($) => token(/%[0-9]/),

    variable_identifier: ($) => /[a-z_][a-z0-9_:]*/i,

    parent: ($) => token(/parent/i),

    function_identifier: ($) =>
      seq(
        optional(seq(field("class", choice($.parent, $.identifier)), "::")),
        field("name", $.identifier),
      ),

    comment: ($) => seq("//", /.*/),

    identifier: ($) => /[a-z_][a-z0-9_]*/i,
  },
});

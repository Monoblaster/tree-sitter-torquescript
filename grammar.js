/**
 * @file The language used in the Torque 3D Game Engine
 * @author Monoblaster
 * @license MIT
 */

/// <reference types="tree-sitter-cli/dsl" />
// @ts-check

module.exports = grammar({
  name: "torquescript",

  extras: ($) => [$.comment, /\s*/],
  word: ($) => $.identifier,

  rules: {
    source_file: ($) =>
      repeat(choice($._statement, $._definition, $._declaration)),

    _definition: ($) => choice($.function_definition),

    function_definition: ($) =>
      seq(
        "function",
        optional(seq(field("namespace", $.name), "::")),
        field("name", $.name),
        $.parameter_list,
        $.block,
      ),

    parameter_list: ($) =>
      seq(
        "(",
        optional($.local_variable),
        repeat(seq(",", $.local_variable)),
        ")",
      ),

    block: ($) => seq("{", repeat($._statement), "}"),

    _declaration: ($) =>
      seq(choice($.package_declaration, $.datablock_declaration), ";"),

    package_declaration: ($) =>
      seq("package", $.name, "{", repeat($._definition), "}"),

    datablock_declaration: ($) =>
      seq(
        "datablock",
        $.name,
        "(",
        $.name,
        optional(seq(":", $.name)),
        ")",
        $.field_block,
      ),

    field_block: ($) => seq("{", repeat($.field), "}"),

    field: ($) => seq($.name, optional($.array), "=", $._expression, ";"),

    _statement: ($) =>
      seq(
        choice(
          $.return_statement,
          $.break_statement,
          $.continue_statement,
          $.switch_statement,
          $.switch$_statement,
          $.for_statement,
          $.while_statement,
          $.if_statement,
          seq(
            choice(
              $.assignment,
              $.function,
              $.method,
              $.auto_assignment,
              $.new,
            ),
            ";",
          ),
        ),
      ),

    break_statement: ($) => seq("break", ";"),

    continue_statement: ($) => seq("continue", ";"),

    return_statement: ($) => seq("return", optional($._expression), ";"),

    switch_statement: ($) =>
      seq("switch", "(", $._expression, ")", $.switch_block),

    switch$_statement: ($) =>
      seq("switch$", "(", $._expression, ")", $.switch_block),

    switch_block: ($) =>
      seq(
        "{",
        repeat(
          seq(
            "case",
            $._expression,
            repeat(seq(choice("or", "and"), $._expression)),
            ":",
            repeat($._statement),
          ),
        ),
        optional(seq("default", ":", repeat($._statement))),
        "}",
      ),

    for_statement: ($) =>
      seq(
        "for",
        "(",
        $._expression,
        ";",
        $._expression,
        ";",
        $._expression,
        ")",
        optional($.block),
      ),

    while_statement: ($) => seq("while", $.expression_block),

    if_statement: ($) =>
      prec.right(
        seq(
          "if",
          $.expression_block,
          optional(seq("else", choice($.block, $._statement))),
        ),
      ),

    expression_block: ($) =>
      seq("(", $._expression, ")", choice($.block, $._statement)),

    _expression: ($) =>
      prec.left(
        choice(
          $.boolean_false,
          $.boolean_true,
          $.name,
          $.number,
          $.string_literal,
          $.tagged_string_literal,
          $.local_variable,
          $.global_variable,
          $.object_field,
          $.assignment,
          $.auto_assignment,
          $.function,
          $.method,
          $.new,
          $.conditional,
          $.grouping,
          $.binary_expression,
          $.unary_expression,
        ),
      ),

    binary_expression: ($) => {
      const table = [
        ["+"],
        ["-"],
        ["*"],
        ["/"],
        ["%"],
        ["||"],
        ["&&"],
        ["|"],
        ["^"],
        ["&"],
        ["=="],
        ["!="],
        [">"],
        [">="],
        ["<="],
        ["<"],
        ["<<"],
        [">>"],
        ["@"],
        ["NL"],
        ["TAB"],
        ["SPC"],
        ["$="],
        ["!$="],
      ];

      return choice(
        ...table.map(([operator]) => {
          return prec.left(
            seq(
              field("left", $._expression),
              // @ts-ignore
              field("operator", operator),
              field("right", $._expression),
            ),
          );
        }),
      );
    },

    unary_expression: ($) => {
      const table = [["-"], ["!"], ["~"]];

      return choice(
        ...table.map(([operator]) => {
          return prec.right(
            seq(field("operator", operator), field("right", $._expression)),
          );
        }),
      );
    },

    grouping: ($) => seq("(", $._expression, ")"),

    conditional: ($) =>
      prec.right(
        seq(
          field("left", $._expression),
          "?",
          field("right", $._expression),
          optional(seq(":", field("else", $._expression))),
        ),
      ),

    assignment: ($) => {
      const table = [
        ["="],
        ["*="],
        ["/="],
        ["%="],
        ["+="],
        ["-="],
        ["&="],
        ["|="],
        ["^"],
        [">>="],
        ["<<="],
      ];

      return choice(
        ...table.map(([operator]) => {
          return prec.right(
            seq(
              field(
                "left",
                choice($.local_variable, $.global_variable, $.object_field),
              ),
              // @ts-ignore
              field("operator", operator),
              field("right", $._expression),
            ),
          );
        }),
      );
    },

    auto_assignment: ($) => {
      const table = [["++"], ["--"]];

      return choice(
        ...table.map(([operator]) => {
          return prec.right(
            seq(
              field(
                "left",
                choice($.local_variable, $.global_variable, $.object_field),
              ),
              // @ts-ignore
              field("operator", operator),
            ),
          );
        }),
      );
    },

    function: ($) =>
      prec(
        2,
        seq(
          optional(seq(field("namespace", $.name), "::")),
          field("name", $.name),
          $.argument_list,
        ),
      ),

    method: ($) =>
      seq($._expression, ".", field("name", $.name), $.argument_list),

    argument_list: ($) =>
      seq("(", optional($._expression), repeat(seq(",", $._expression)), ")"),

    new: ($) =>
      seq(
        "new",
        field("name", $.name),
        "(",
        optional($._expression),
        ")",
        optional($.field_block),
      ),

    local_variable: ($) => seq("%", field("name", $.name), optional($.array)),

    global_variable: ($) =>
      seq(
        "$",
        field("name", $.name),
        repeat(seq("::", $.name)),
        optional($.array),
      ),

    object_field: ($) =>
      seq($._expression, ".", field("name", $.name), optional($.array)),

    array: ($) => seq("[", $._expression, repeat(seq(",", $._expression)), "]"),

    number: ($) => choice(/\d+/, /\d+\.\d+/, /\d+\.\d+E\-\d+/, /0x[0-9&&a-f]+/),

    string_literal: ($) =>
      seq(
        '"',
        repeat(
          choice(
            alias(token.immediate(/[^\\"\n]+/), $.string_content),
            $.markup,
            $.escape_sequence,
          ),
        ),
        '"',
      ),

    tagged_string_literal: ($) =>
      seq(
        "'",
        repeat(
          choice(
            alias(token.immediate(/[^\\'\n]+/), $.string_content),
            $.markup,
            $.escape_sequence,
          ),
        ),
        "'",
      ),

    escape_sequence: ($) =>
      token(prec(1, seq("\\", choice(/[^cx]/, /c[rpo0-9]/, /x[a-z]{2}/)))),

    markup: ($) =>
      token(
        seq(
          "<",
          choice(
            "font",
            "color",
            "shadow",
            "shadowcolor",
            "just",
            "clip",
            "lmargin%",
            "rmargin%",
            "lmargin",
            "rmargin",
            "a",
            "/a",
            "linkcolor",
            "bitmap",
            "tab",
            "spush",
            "spop",
            "sbreak",
            "br",
            "div",
            "tag",
          ),
          /.+/,
          ">",
        ),
      ),

    identifier: ($) => /[a-z]*/,

    boolean_true: ($) => token(/true/i),

    boolean_false: ($) => token(/false/i),

    parent: ($) => /parent/i,

    name: ($) => /[a-z_][a-z0-9_]*/i,

    comment: ($) => /\/\/.*/,
  },
});

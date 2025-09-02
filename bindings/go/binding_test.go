package tree_sitter_torquescript_test

import (
	"testing"

	tree_sitter "github.com/tree-sitter/go-tree-sitter"
	tree_sitter_torquescript "github.com/tree-sitter/tree-sitter-torquescript/bindings/go"
)

func TestCanLoadGrammar(t *testing.T) {
	language := tree_sitter.NewLanguage(tree_sitter_torquescript.Language())
	if language == nil {
		t.Errorf("Error loading Torque Script grammar")
	}
}

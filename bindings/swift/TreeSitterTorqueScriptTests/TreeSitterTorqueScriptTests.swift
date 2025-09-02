import XCTest
import SwiftTreeSitter
import TreeSitterTorquescript

final class TreeSitterTorquescriptTests: XCTestCase {
    func testCanLoadGrammar() throws {
        let parser = Parser()
        let language = Language(language: tree_sitter_torquescript())
        XCTAssertNoThrow(try parser.setLanguage(language),
                         "Error loading Torque Script grammar")
    }
}

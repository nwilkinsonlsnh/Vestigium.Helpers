# Kql — Design 1.1 (as built)

Lexer → parser → binder → evaluator. Catalog is pack + group. Session builds a case-insensitive lookup of canonical, suffix, and aliases. Compile returns `Ok` / `Error` / `Diagnostics`.

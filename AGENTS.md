# Project instructions

## CodeGraph
If a `.codegraph/` directory exists at the repository root, use CodeGraph before text search or reading files to locate/understand code. Use `codegraph explore` or `codegraph node` (or equivalent MCP tools). If no index exists, skip CodeGraph; do not index without the user's decision.

## Project
- Unity 6000.4.2f1; target WebGL. Keep the Unity project at this root.
- Read Docs/NGHIEN_CUU_BUG_VILLAGE.md for research and deliberate differences from the original game.
- Gameplay domain rules are in Assets/Scripts/Domain; run Tools/DomainChecks after changing economy, worker allocation, time or placement rules.
- Original Blender source lives in ArtSource, exported FBX in Assets/Resources/Models. Record third-party asset sources and licenses in Docs/ASSETS.md if assets are added later.
- Do not mistake the Blender art preview for a Unity gameplay screenshot.
- Keep English as the default interface language; support Vietnamese through the shared `Assets/Scripts/Localization/I18n.cs` catalog. Route new user-facing text through this catalog, preserving placeholders in both languages. Do not change existing save semantics without migration or explicit reset UI.

## Code quality and documentation
- Write clean, readable, maintainable C# that is easy to extend. Follow `.editorconfig` and the surrounding code conventions.
- Use descriptive names for classes, methods, parameters, and local variables. Write one statement per line, use braces for control flow, and split long expressions into readable lines.
- Keep classes focused on a clear responsibility and methods focused on a single task. Extract cohesive helpers when needed; avoid unnecessary abstractions and duplicated logic.
- Keep gameplay rules in the domain layer and presentation/input concerns in the Unity layer. Extend existing systems through clear boundaries rather than adding unrelated responsibilities to large classes.
- Every new class and method, including private helpers and constructors, must have an English XML documentation block with at least `<summary>`. Add `<param>`, `<returns>`, and `<remarks>` when they clarify inputs, outputs, constraints, or side effects. Use an English block comment for local functions where XML documentation is unsupported.
- Keep documentation accurate when changing existing classes or methods. Explain intent and behavior rather than merely repeating the identifier.
- Readability refactors must preserve gameplay behavior, serialized field names, enum IDs, save compatibility, and Unity lifecycle callbacks unless the task explicitly requires changing them. Run the relevant existing checks after refactoring.

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

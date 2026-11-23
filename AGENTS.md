# AGENTS.md — Instructions for all AI agents working in this repository

This is a Unity project (`DungeonBuilder`, Unity 6000.4.0f1) that uses the
**Unity-StyleCop** package. Read and follow every rule below before you propose
or make any change. If an instruction here conflicts with a user's direct
request, follow the user — but say so explicitly in your plan.

---

## 1. Adhere to Unity StyleCop

All C# code must satisfy the ruleset shipped with the Unity-StyleCop package:

```
Packages/Unity-StyleCop/RequiredPackages/UnityStyleCop.ruleset
```

That ruleset is applied to every generated project (`Assembly-CSharp.csproj`,
`Assembly-CSharp-Editor.csproj`, `Assembly-CSharp-firstpass.csproj`,
`StyleCop.Analyzer.csproj`). The only project-level exception is configured in
`.editorconfig`:

- `SA1305` (Hungarian notation on fields, e.g. `m_Name`) — disabled.

Practical consequences you must respect in any code you touch:

- Every type, member and field has an XML documentation header (`/// <summary>`)
  with the SA16xx requirements: `<param>`, `<returns>`, `<typeparam>`, and
  `<inheritdoc/>` for overrides/interface implementations.
- One type per file, file name matches the type name, `using` directives outside
  the namespace and sorted (`System` first).
- Namespaces match folder structure; access modifiers are always explicit —
  `public`, `private`, `protected` and `internal` are written out (never omit).
- Braces on their own line (Allman); a single space after keywords (`if (x)`);
  no trailing whitespace; file ends with exactly one newline.
- `this.` prefix on instance member access (SA1101 is enabled).
- Elements ordered: constants → fields → constructors → properties → methods;
  static before instance, public before private.
- Comments start with a capital letter and end with a period; TODOs are
  formatted `// TODO: ...`.
- Use `string.Empty` rather than `""`, `default(T)` rather than literal zeroes,
  and no regions (SA1124 is enabled).
- File headers are not required (SA1633 is set to `None`).
- Do not add `#pragma warning disable` or per-file suppressions to silence
  StyleCop. Fix the code, or ask the user before touching the ruleset.

Match the style of the surrounding file even when it is stricter than the
minimums above. New code should not introduce a single new StyleCop warning.

Also follow Unity conventions: `[SerializeField] private` fields over public
ones, `Time.deltaTime` in `Update`, `GetComponent` cached in `Awake`, and
`#if UNITY_EDITOR` guards around editor-only API use in runtime scripts.

---

## 2. Always ask before editing files

You may **read, search and analyse freely**. You may not edit, create, move,
rename or delete a file until the user has approved the specific change.

Before applying anything, present a proposal in this exact shape:

```
### Files to edit
- Assets/Scripts/.../BuilderInventory.cs
- Assets/Scripts/.../PickTool.cs

### Methods to edit
- BuilderInventory.SelectItem(int index) — lines 84–112
- PickTool.HandleClick(Vector2 worldPos) — lines 40–58

### Exact changes
BuilderInventory.cs, lines 91–95
-   thumbnail.image.rectTransform.sizeDelta = new Vector2(48f, 48f);
+   thumbnail.image.rectTransform.sizeDelta = FitPreservingAspect(48f, sprite);
  (and a new private static Vector2 FitPreservingAspect(...) below SelectItem)
```

Rules for the proposal:

- Be exhaustive and precise: file path, method signature, the line numbers or the
  exact before/after text of every hunk. No "and similar places" hand-waving.
- State explicitly if a change also touches Unity `.meta` files, prefabs, scenes,
  asmdefs or ProjectSettings — and wait for a separate approval for those.
- Explain *why* in one or two sentences, and flag anything you are unsure about
  instead of guessing.
- After approval, apply only the approved hunks. If you find you need a hunk that
  was not in the proposal, stop and ask again.
- When a request is ambiguous, ask a question rather than choosing a direction
  and editing.

---

## 3. Never touch Git

Git is the user's alone. Do not run, at any point and for any reason:

- `git add`, `git commit`, `git reset`, `git checkout`, `git switch`, `git restore`
- `git stash`, `git clean`, `git rebase`, `git merge`, `git cherry-pick`
- `git push`, `git pull`, `git fetch`, `git tag`, `git branch`
- `git config`, `git remote`, `git submodule`, `git worktree`
- any wrapper or script (including hooks, `gh`, or IDE commands) that performs the above

Do not stage, unstage, stage-split, write hooks, edit `.gitignore` /
`.gitattributes` / `.mailmap`, resolve conflicts, or authored commit messages
unless the user explicitly asks for text they will paste themselves. Read-only
inspection (`git status`, `git diff`, `git log`, `git blame`) is allowed when it
helps you understand the code, but never as a step towards changing repository
state. **Never create a commit.** The user commits.

---

## 4. Never touch assets — only scripts

Everything under `Assets/` that is **not** a C# script is the user's asset and is
off-limits. Do not create, edit, move, rename, delete, reimport or regenerate:

- prefabs, scenes, ScriptableObjects, `.asset`, `.unity`, `.prefab`, `.mat`
- textures, sprites, sprite atlases, `.png`, `.psd`, `.aseprite`, `.tga`
- animations, controllers, `.anim`, `.controller`, `.overrideController`
- audio, fonts, shaders, `.shader`, `.shadergraph`, VFX graphs
- text assets, `.json`, `.csv`, `.txt` data files, localization tables
- `.meta` files, asmdefs, `Packages/`, `ProjectSettings/`, `UserSettings/`,
  `Assembly-CSharp*.csproj`, `DungeonBuilder.sln`
- anything in `Library/`, `Temp/`, `obj/`, `build/`, `Logs/`

You **may** work on:

- `.cs` files under `Assets/` (Unity scripts and editor scripts)
- documentation markdown at the repository root (`README.md`, `AGENTS.md`, etc.)
  when the user asks for it

The only exception is a **clear, explicit instruction** to change a specific
non-script asset — for example "add the sprite to this prefab". Even then,
touch only the named asset, confirm the exact file path with the user first, and
prefer changing data through C# / editor tooling if that is an option.

Never generate new binary assets, run `AssetDatabase.Refresh` on the user's
behalf without approval, or "fix" import settings.

---

## 5. Working checklist

1. Read the relevant code and its neighbours; check existing style.
2. Write out the proposal: files, methods, exact line-level changes.
3. Wait for approval.
4. Apply exactly the approved changes, StyleCop-clean.
5. Report what changed, what you could not verify, and stop — do not start
   refactoring, committing or tidying.

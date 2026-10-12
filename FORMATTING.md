# Formatting Instructions

The repository's formatting rules are stored in the root `.editorconfig` file.
Compatible editors apply these rules automatically.

## Project style

- C# and Razor files use 4 spaces for indentation.
- JavaScript, JSON, CSS, and HTML files use 2 spaces.
- Do not use tabs.
- Remove trailing whitespace.
- End files with a newline.
- C# opening braces go on a new line.
- Interface names begin with `I`.

## Editor setup

Make sure your editor supports EditorConfig. In VS Code, install an
EditorConfig-compatible extension if necessary and enable **Format on Save**.
You can also format the current document with `Shift + Alt + F`.

## Before committing C# changes

Run these commands from the repository root:

```powershell
dotnet format ARTISTO.csproj --no-restore
dotnet format ARTISTO.csproj --verify-no-changes --no-restore
dotnet build --no-restore
```

The verification command should finish without formatting errors.

## Before committing front-end changes

Run these commands from the repository root:

```powershell
cd ClientApp
npm run lint
npm test -- --runInBand
cd ..
```

The `caniuse-lite is outdated` notice does not mean that lint failed.

## Final checks

Before committing, check the changes:

```powershell
git diff --check
git status --short
git diff
git diff --cached
```

Only format files related to your work. Avoid unrelated formatting changes in
feature pull requests because they make reviews harder and can cause merge
conflicts.

# Progression Systems Demo

A small Unity project that turns two common mobile-game requirements into
reviewable, tested systems: durable XP progression and command-aware path
navigation.

The project is an independent technical demo by Oguzhan Ozdemir. It is not an
application assignment, studio product or claim of affiliation with another
company.

![Progression Systems Demo](Docs/progression-systems-demo.png)

## What to review

- A pure C# progression model handles multi-level grants, overflow, invalid
  input and a configurable level cap.
- A deterministic command queue keeps repeated advance requests in order while
  a finish request replaces stale pending work.
- The Unity presenter owns animation and input only; domain behavior remains
  testable without scene timing.
- Local persistence stores a small versionable snapshot rather than exposing
  mutable UI state.
- The demo UI is generated from Unity primitives and carries no third-party art
  or runtime package.

See [the architecture note](Docs/ARCHITECTURE.md) for boundaries and production
tradeoffs.

## Run

1. Install Unity **2022.3.61f1** with macOS build support.
2. Open the repository folder. Unity creates the lockfile and imports the four
   declared packages.
3. Choose **Demo > Create clean scene**, then enter Play Mode.
4. Use **Advance**, **Finish path** and **+350 XP**. Keyboard shortcuts are
   Space, F and X.

The scene contains no serialized gameplay state. A runtime bootstrap creates
the interface, which makes a clean clone reproducible.

## Test and build

Run EditMode tests from **Window > General > Test Runner**, or on macOS:

```sh
UNITY="/Applications/Unity/Hub/Editor/2022.3.61f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform editmode \
  -testResults "$PWD/Builds/editmode-results.xml" -quit

"$UNITY" -batchmode -projectPath "$PWD" -buildTarget StandaloneOSX \
  -executeMethod OguzhanOzdemir.ProgressionDemo.Editor.DemoProjectBuilder.BuildMac \
  -buildOutput "$PWD/Builds/macOS/ProgressionSystemsDemo.app" -quit
```

## Earlier exercise

This clean-room demo develops the progression and queued-navigation ideas from
an older personal Unity exercise. The previous repository remains private as an
archive because it contains unnecessary third-party packages and task-specific
assets. No company branding or implication of employment is used here.

MIT licensed.

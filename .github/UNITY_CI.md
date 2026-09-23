# Unity CI setup

The repository now has `.github/workflows/unity-ci.yml`.

Required repository secret:
- `UNITY_LICENSE`: Unity Personal/Pro license file content accepted by GameCI.

The workflow runs on pushes and pull requests targeting `main`, and can also be started manually. It performs Unity EditMode/PlayMode test validation and uploads test artifacts.

If the project later adds Android build credentials, Android packaging should be a separate release workflow rather than making every pull request perform a full APK/AAB build.

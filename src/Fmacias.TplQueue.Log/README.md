# TplQueue.Log
Deterministic log library for classifying log entries and messages.

## Workspace solution (optional)
This repo builds standalone. If you also clone the umbrella workspace `WorkspaceTplQueue`, this repo will automatically import the shared `Directory.Build.props` from `..\\WorkspaceTplQueue\\Directory.Build.props` via its local `Directory.Build.props`.
The import is conditional; if the workspace folder is not present, nothing changes.

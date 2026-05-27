# First Job Graph

TplQueue favors explicit graphs over hidden callback chains.

Build a rooted `Extract -> Transform -> Load` graph with `IJob` and `IJobRoot`, then enqueue the root.

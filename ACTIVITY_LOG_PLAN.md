# Activity Log To-Do

- [ ] Obtain authorization to deploy.
- [ ] At deployment, follow the [activity log upgrade guide](docs/upgrade-activity-log-events.md). Clear pre-release activity history before starting the new host. Run the schema upgrade on legacy tables, or use the one-time reset for an already-upgraded preview. Preserve all other application data.
- [ ] Verify that the release starts with empty activity history and that a new administrator action appears in the log.

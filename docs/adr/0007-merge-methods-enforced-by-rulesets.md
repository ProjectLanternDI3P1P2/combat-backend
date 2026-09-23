# Merge methods are enforced per branch by rulesets

The rule in [ADR-0002](./0002-merge-strategy-depends-on-the-target-branch.md) could
only ever be a convention: GitHub configures allowed merge methods per repository,
not per branch. One ruleset targeting both `dev` and `main` now enforces it — merge
commits only — so a pull request can no longer be squashed or rebased by accident.

## Consequences

The automatic back-merge ([ADR-0003](./0003-automatic-back-merge-from-main-to-dev.md))
merges `main` into `dev` with a merge commit, which the ruleset already allows. No
bypass is needed: nobody, admins included, sees a squash or rebase button on either
branch.

Squash and rebase stay disabled at repository level too, so the ruleset and the
repository settings say the same thing.

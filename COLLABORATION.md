# Collaboration

Every change goes through an issue, a branch and a pull request, so it can be read and discussed before it reaches
`main`. `main` is protected: it only changes through a pull request with a green pipeline.

All commands work in Windows PowerShell. `N` is the issue number (shown with `#` in the issue list).

1. **Add an issue**, e.g. "update controller method to handle missing …".
2. **Write a short list of requirements** in the issue: what must change, and why.
3. **See the open issues:**

   ```sh
   gh issue list
   ```

4. **Assign the issue to yourself:**

   ```sh
   gh issue edit N --add-assignee "@me"
   ```

5. **Create a branch for the issue and check it out:**

   ```sh
   gh issue develop N --checkout
   ```

6. **When the issue is done, commit and push.** The commit message starts with its type (`feat:`, `fix:`,
   `docs:`, `chore:`, `ci:`) and tags the issue:

   ```sh
   git add .
   ```

   ```sh
   git commit -m "fix: update workspace controller to fix X #N"
   ```

   ```sh
   git push
   ```

7. **Open a pull request.** `Closes #N` closes the issue automatically when the pull request is merged:

   ```sh
   gh pr create --fill --body "Closes #N"
   ```

   Review it on GitHub under *Files changed* and discuss it before merging.

8. **Merge the pull request.** It is squash merged (one commit on `main`) and the branch is deleted:

   ```sh
   gh pr merge --squash --delete-branch
   ```

9. **Switch back to `main` and clean up:**

   ```sh
   git switch main; git pull --prune
   ```

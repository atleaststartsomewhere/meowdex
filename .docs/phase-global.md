# Phase Global Notes

## Windows Trust / SmartScreen
Unsigned Windows executables trigger "Unknown publisher" warnings.  
To reduce this in production, use code signing.

### Recommended Path
1. Acquire a Windows code-signing certificate:
   - `OV` (lower cost, reputation builds over time)
   - `EV` (higher trust/reputation behavior)
2. Sign release binaries (`.exe`) and installer artifacts (`.msi`/`.msix`) with `signtool`.
3. Prefer distributing a signed installer over raw unsigned exe-in-zip.
4. Maintain consistent publisher identity so SmartScreen reputation can accumulate.

### Operational Impact
- Without signing: users will often need `More info` -> `Run anyway`.
- With signing: warnings decrease, but OV may still warn initially until reputation builds.
- EV generally improves first-run trust outcomes.

### Future Work
- Add signing step to publish pipeline/script.
- Store signing configuration in release docs and CI secrets handling.

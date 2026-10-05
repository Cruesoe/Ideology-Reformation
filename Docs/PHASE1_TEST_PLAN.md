# Phase 1 In Game Test Plan

## Setup

1. Enable Development mode and start or load a colony whose player ideoligion is Fluid.
2. In Mod settings, enable Development diagnostics. Preservation always runs in Preserve Where Possible behavior; there is no mode to set.
3. Use Debug actions, Ideoligion, Max development points.
4. Open the Ideoligions tab and begin a reform.

## Core transaction tests

1. Change a precept, then attempt to change a different issue. The second change must be rejected and the status line must name the locked object.
2. Edit a ritual's date, attached reward and name in one edit. All changes must be accepted as one ritual object.
3. Edit only names, descriptions, colours, icons and style categories. No mechanical object should be selected.
4. Select a mechanical object, make cosmetic changes elsewhere, and verify that the cosmetics remain allowed.
5. Press Reset / discard changes from each editor stage. All mechanical and cosmetic changes must return to their opening values, with development points untouched.
6. Close the reform without confirmation, reopen it, and verify that no abandoned change reached the real ideology.

## Meme and preservation tests

1. Add one meme. Verify that the review lists the meme as primary and any automatic precept changes as consequences.
2. Remove one meme. Verify that unrelated rituals, roles, buildings, relics, weapons, venerated animals and appearance choices retain their IDs and configuration when still valid.
3. Attempt a direct remove-and-add replacement. It must be rejected as two reforms.
4. Test a meme change at the vanilla meme cap and confirm behavior remains vanilla.

## Review and commitment tests

1. Complete a one-precept reform. The Ideology Reformation review must appear even when vanilla has no special warning.
2. Choose Go back, alter the same object, and review again.
3. Confirm a reform which also produces a vanilla lost-precept warning. Cancel that warning, return to the editor and confirm again; the mod review must be shown again.
4. Complete final confirmation. Development points must reset and reform count must increase exactly once.
5. Save and reload. The committed result must persist without any mod-owned ideology state.

## Scope tests

1. View or dev-edit a Fixed ideoligion. Ideology Reformation must not create a reform session or change its behavior.
2. Add the mod to a save containing established ideologies. Loading the save must not validate, regenerate or remove existing content.
3. Test with at least one mod-added ritual, role and precept before Phase 2 work begins.


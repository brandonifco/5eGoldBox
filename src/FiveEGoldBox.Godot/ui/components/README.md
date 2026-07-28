# Reusable UI Component Contracts

These contracts are UI-owned. Callers supply display values and availability; components render those values, manage their own visual children, and emit UI intent through callbacks or signals. Components do not calculate gameplay legality, rules outcomes, persistence state, or backend identifiers.

## HeaderBar

- Inputs: `HeaderText`, `Immersive`.
- Updates: `SetLocationAndMode(location, mode)`.
- Output: none.
- Ownership: header panel, margin, typography, and formatted location/mode text.

## PartySidebar

- Inputs: `TitleText`, `Immersive`, `PartyMemberRowScene`.
- Updates: `ClearMembers()`, `AddMember(memberName, healthText)`.
- Readback: `GetMembers()` for UI-state mirroring only.
- Ownership: title, scrolling, member-list container, and `PartyMemberRow` lifecycle.

## PartyMemberRow

- Inputs: `MemberName`, `HealthText`.
- Updates: `Configure(memberName, healthText)`.
- Output: none.
- Ownership: member-name and health-label presentation.

## MessageLog

- Inputs: `TitleText`, `MessageText`, `Scrollable`.
- Updates: `SetMessage(message)`.
- Output: none.
- Ownership: title, message rendering, fit behavior, and scrolling presentation.

## CommandBar

- Inputs: `Immersive`.
- Updates: `Clear()`, `AddContent(content)`.
- Output: delegated to hosted command controls.
- Ownership: command panel, margin, row layout, and hosted-control lifecycle.

## HotkeyCommandButton

- Inputs and updates: `Configure(commandName, formattedLabel, shortcutKey, handler, enableShortcut)`.
- Output: invokes the supplied UI intent callback.
- Ownership: formatted hotkey label, tooltip, shortcut resource, and pressed-handler replacement.

## SelectionList

- Inputs: `TitleText`, `EmptyText`, `AutoSelectFirst`, `SelectionListEntry` values.
- Updates: `SetItems(entries)`, `ClearItems()`, `SelectIndex(...)`, `SelectId(...)`, `FocusSelected()`.
- Outputs: `SelectionChanged(index, itemId)`, `SelectionActivated(index, itemId)`.
- Ownership: item-button lifecycle, selection state, disabled skipping, focus navigation, wrapping, and scrolling.

## ConfirmationDialog

- Inputs and updates: `ShowDialog(title, message, confirmText, cancelText, kind, confirmEnabled)`, `Confirm()`, `Cancel()`, `CloseDialog()`.
- Outputs: `Confirmed`, `Cancelled`, `ResultSubmitted(confirmed)`.
- Ownership: confirmation-card lifecycle, safe initial focus, button availability, and cancellation routing through `ModalBackdrop`.

## TooltipPanel

- Inputs: `TitleText`, `BodyText`.
- Updates: `SetContent(title, body)`, `ShowTooltip(title, body)`, `HideTooltip()`.
- Output: none.
- Ownership: tooltip panel, title visibility, body wrapping, and visibility state.

## StatusBadge

- Inputs: `StatusText`, `StatusBadgeKind`.
- Updates: `SetStatus(statusText, kind)`.
- Output: none.
- Ownership: status text, semantic theme variation, and descriptive tooltip text.

## ModalBackdrop

- Inputs: `DismissOnBackdropPressed`, `DismissOnEscape`, modal content, optional initial focus.
- Updates: `SetModalContent(content)`, `ClearModalContent()`, `ShowModal(content, initialFocus)`, `HideModal()`, `CloseModal()`.
- Outputs: `DismissRequested`, `Opened`, `Closed`.
- Ownership: full-screen input blocking, safe-area content host, focus trapping, content lifecycle, and previous-focus restoration.

## Layout boundaries

`StandardLayout` and `ImmersiveLayout` expose their reusable chrome and presentation hosts through typed read-only properties. `AppShell` coordinates those boundaries and does not bind or construct repeated component internals.

## Validation boundary

Components accept UI-local strings, booleans, local IDs, callbacks, and UI-only enums. They may validate their own structural preconditions, such as non-empty list IDs or parentless modal content. They must not inspect character statistics, calculate command legality, reproduce rules, or depend on `FiveEGoldBox.Application` or `FiveEGoldBox.Core`.

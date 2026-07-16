# Changelog

## Unreleased

- Keep dismissed meetings visible in expanded and Win+W agendas while excluding them from taskbar/current surfaces, notifications, and auto-join; use volume-off indicators with an inline unmute action, prevent muting ended events, and restore them automatically at meeting end.
- Add selectable multi-point meeting notifications, including meeting start, and configurable taskbar active states with native-hover or animated-shine styling.
- Add an optional native Windows 11 Win+W provider with current/next meeting, today's agenda, live countdowns, meeting colors, and Join/Open/Refresh actions.
- Add native Windows MeetingBar with tray, near-tray, and far-left taskbar surfaces.
- Add Classic Outlook, ICS, and direct Google Calendar providers.
- Add meeting detection, popup, filters, reminders, auto-join, bookmarks, startup, hotkey, and launcher commands.
- Improve settings dropdown contrast and add guided Google OAuth setup.
- Embed the MeetingBar icon in the executable, windows, and tray surface.
- Detect missing Google Calendar scope with actionable recovery guidance and persistent diagnostics.
- Keep the taskbar widget visible after taskbar movement, auto-hide, or accidental hiding.
- Explain disabled Google Calendar API and distinguish it from missing OAuth permission.
- Reassert widget z-order so Windows taskbar redraws cannot cover it.
- Add a Mac-style event timeline, Google event colors, emoji-safe titles, and an Open calendar action; make taskbar widgets content-sized and remove Quit from the popup.
- Expand long-title widgets, keep them above taskbar activations, add automatic/custom taskbar-matched colors and a toggleable event-colored icon, render full-color emoji, and replace event Join/X controls with meeting-provider logos.
- Add configurable ongoing time remaining, accurate dimmed past events, transparent emoji/provider art, taskbar-derived widget hover, auto-hiding overlay scrollbars, and side detail flyouts on event hover.
- Match the taskbar widget and popup to Windows 11 with native acrylic, the current Windows accent, rounded native corners, taskbar spacing, and open/close motion; replace utility glyphs with Lucide icons, suppress hover tooltips, fix widget text clipping, and simplify its event indicator.
- Add event-aware tray icon colors and a 30-minute upcoming dot, make the taskbar widget borderless and transparent until native-style hover, match the local Windows 11 Start-menu slide timing, and keep Desktop Acrylic active across focus and composition changes.
- Move popup motion off UI-thread layout onto a DWM-synchronized native-window animation, use an always-on native acrylic composition tint with an opaque fallback, and reduce automatic widget hover to the measured Windows taskbar intensity.
- Pre-render the popup at startup, show a calendar-loading state until the first sync completes, make repeated tray/widget clicks reliably toggle it closed, and neutralize the timeline background and brighter current-time divider.
- Match the overlay scrollbar and event hover details to the neutral acrylic palette, and add short eased mouse-wheel scrolling while preserving direct precision-touchpad deltas.
- Synchronize wheel scrolling to the display refresh, remove the popup header, move Refresh and Settings into its footer, make the full taskbar-widget height interactive, and redesign Settings around native Windows colors and navigation without closing after Save.
- Replace the WPF settings window with a real WinUI 3 host using native NavigationView, Mica, Fluent cards, OS theme/accent/high-contrast resources, localized controls, and composition scrolling while preserving Save/reload, Google OAuth, calendar selection, and every setting.
- Overlay and animate the popup scrollbar without reserving icon space, use the meeting-provider logo in the primary Join button, hide empty bookmarks, add separate past/future hour and day-boundary controls shared by the event list and timeline, and add a local-only WinUI Debug page for injected events, notifications, and reminders.
- Tighten WinUI settings parity with centered equal-width pages, unified selectors, right-aligned Ein/Aus toggles, renamed taskbar sides, an always-masked Google client secret, and no redundant back or search controls.

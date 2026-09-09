---
name: web-design-guidelines
description: Review UI code for Web Interface Guidelines compliance. Use when asked to "review my UI", "check accessibility", "audit design", "review UX", or "check my site against best practices".
metadata:
  author: vercel
  version: "1.0.0"
  argument-hint: <file-or-pattern>
---

# Web Interface Guidelines

Review files for compliance with Web Interface Guidelines.

## SWApuestas MVC responsive review

When reviewing ASP.NET MVC or Razor views, explicitly verify responsive behavior in addition to the fetched guidelines:

- Views work at mobile, tablet, and desktop widths without unintended horizontal scrolling.
- Navigation, forms, filters, alerts, validation messages, tables, pagination, dialogs, and primary actions remain usable on small screens.
- Tables use an intentional responsive strategy such as selected columns, wrapping, controlled overflow, or a mobile layout; important content must not disappear.
- Fixed widths, viewport-dependent text overflow, clipped controls, overlapping content, and breakpoint-specific regressions are findings.
- Touch targets, keyboard focus, contrast, readable text, and error states remain accessible at every supported width.
- Responsive styles follow the existing MVC/Bootstrap design system and do not introduce conflicting frameworks or ad hoc breakpoints.

## How It Works

1. Fetch the latest guidelines from the source URL below
2. Read the specified files (or prompt user for files/pattern)
3. Check against all rules in the fetched guidelines
4. Output findings in the terse `file:line` format

## Guidelines Source

Fetch fresh guidelines before each review:

```
https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md
```

Use WebFetch to retrieve the latest rules. The fetched content contains all the rules and output format instructions.

## Usage

When a user provides a file or pattern argument:
1. Fetch guidelines from the source URL above
2. Read the specified files
3. Apply all rules from the fetched guidelines
4. Output findings using the format specified in the guidelines

If no files specified, ask the user which files to review.

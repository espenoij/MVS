---
name: MVS
description: Native WPF verification workspace in the SES visual language.
colors:
  energy-green: "#3DE6A9"
  energy-green-hover: "#2FCF97"
  energy-green-pressed: "#17A377"
  green-text: "#12805B"
  green-tint: "#E4FBF3"
  blue-grey: "#334A5C"
  blue-grey-hover: "#3F5A6C"
  blue-grey-pressed: "#263A48"
  blue-grey-light: "#4E6B80"
  blue-grey-tint: "#EBEFF2"
  navigation-selected: "#356359"
  surface: "#F5F8FA"
  surface-alt: "#EAEFF2"
  white: "#FFFFFF"
  border: "#D5DDE2"
  text-secondary: "#566672"
  neutral: "#6B7A85"
  warning: "#EBBA47"
  error: "#D5303E"
  graph-reference: "#1A6EBF"
  graph-vessel: "#E07B10"
  graph-deviation: "#8040B0"
typography:
  title:
    fontFamily: "Roboto Condensed, Arial, sans-serif"
    fontSize: "28px"
    fontWeight: 700
  subheading:
    fontFamily: "Roboto Condensed, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 700
  body:
    fontFamily: "Roboto Condensed, Arial, sans-serif"
    fontSize: "14px"
    fontWeight: 400
  label:
    fontFamily: "Roboto Condensed, Arial, sans-serif"
    fontSize: "14px"
    fontWeight: 600
rounded:
  control: "3px"
  wizard: "4px"
spacing:
  xsmall: "4px"
  small: "8px"
  medium: "16px"
  large: "24px"
components:
  button-primary:
    backgroundColor: "{colors.energy-green}"
    textColor: "{colors.blue-grey}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "6px 16px"
  button-primary-hover:
    backgroundColor: "{colors.energy-green-hover}"
  button-primary-pressed:
    backgroundColor: "{colors.energy-green-pressed}"
  button-secondary:
    backgroundColor: "{colors.blue-grey}"
    textColor: "{colors.white}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "6px 16px"
  button-secondary-hover:
    backgroundColor: "{colors.blue-grey-hover}"
  button-secondary-pressed:
    backgroundColor: "{colors.blue-grey-pressed}"
  watermark-field:
    backgroundColor: "{colors.white}"
    textColor: "{colors.blue-grey}"
    typography: "{typography.body}"
    padding: "4px 6px"
  navigation-item:
    backgroundColor: "{colors.blue-grey}"
    textColor: "{colors.white}"
    padding: "8px 12px"
  navigation-item-selected:
    backgroundColor: "{colors.navigation-selected}"
    textColor: "{colors.white}"
    padding: "8px 12px"
  subtab-selected:
    backgroundColor: "{colors.white}"
    textColor: "{colors.green-text}"
    padding: "8px 12px"
  wizard-current:
    backgroundColor: "{colors.blue-grey}"
    textColor: "{colors.white}"
    rounded: "{rounded.wizard}"
    padding: "8px 10px"
---

# Design System: MVS

## Overview

**Creative North Star: "The Verification Workspace"**

The implemented shell combines dark blue-grey navigation with a cool, light workspace and compact controls for the existing verification workflow. The Helideck platform's transfer document, current source, and an older synthetic-data browser-test capture informed this revision; this record is not independent visual approval.

**Key Characteristics:**
- Native WPF controls and data-dense grids.
- Blue-grey structure, mint interaction accents, and explicit status colors.
- Bundled condensed type and small, bordered surfaces.

## Colors

The palette pairs cool industrial neutrals with mint and functional status colors.

### Primary
- **Energy green:** primary buttons, active accents, and positive sensor states; darker variants mark hover, press, or text on light surfaces.
- **Blue-grey:** navigation, grid headers, primary text, and secondary actions; darker and lighter variants distinguish states.

### Secondary
- **Status amber and red:** warning and error states, including sensor rows and application banners.
- **Chart blue, orange, and violet:** reference MRU, vessel MRU, and mean deviation respectively; the analysis legend names each series.

### Neutral
- **Cool surface and alternate surface:** workspace and quiet control or legend backgrounds.
- **White and border:** data grids and fields with fine separators; secondary and neutral text soften supporting information.

**The Status Is Named Rule.** Where status is shown in a project list or chart legend, pair color with the existing text label rather than relying on color alone.

## Typography

Roboto Condensed is bundled as a WPF resource, with Arial fallback. MainWindow inherits the family and implicit TextBlock and Label styles apply it elsewhere; individual screens still override size and color.

The observed hierarchy includes a 28-DIP bold project heading, 20-DIP setup heading, 16-DIP bold axis headings, 14-DIP body and semibold control labels, and a 22-DIP bold correction value. Smaller local text also exists; it is not a universal body size. The resource dictionary defines a 28/20/16/14/12 scale, but named text styles are not established as universal usage.

## Layout

WPF dimensions are device-independent pixels; frontmatter pixel values represent the corresponding 96-DPI units. The maximized main window uses a 288-DIP left tab rail, an 80-DIP collapsed rail, and an expanding workspace. Project setup uses a five-column wizard bar, then a 2:3 split with a 350-DIP minimum on the project-list column and a scrollable details column. On narrow windows the setup split scrolls horizontally rather than clipping the fields. Input setup uses two equal columns. Common margins are 8, 16 and 24 DIP. No responsive breakpoint system is defined in the sampled WPF source.

## Elevation & Depth

The shared native templates and sampled screens use flat fills, borders, and tonal changes rather than a shared shadow vocabulary. Native controls without custom templates can still depend on the Windows theme.

## Shapes

Buttons and the chart legend use a slight 3-DIP corner; wizard steps use 4 DIP. Fields and data grids are rectangular with 1-DIP borders; the toggle uses a 24-DIP-high rounded track. Status badges can be more rounded.

## Components

### Buttons
Primary buttons use mint with blue-grey text; secondary buttons invert to blue-grey with white text. Both are semibold at 14 DIP, with 16 horizontal and 6 vertical padding, a 34-DIP minimum height and 120-DIP minimum width unless overridden. Hover and pressed colors darken; keyboard focus changes the border to 2 DIP. Disabled buttons use alternate surface, border, and neutral text.

### Inputs
The watermark field is white with a 1-DIP border and 6-by-4-DIP padding; its empty watermark uses secondary text. Keyboard focus changes its border to mint; disabled opacity is 0.5. The shared combo box has the same surface and padding with a 34-DIP minimum height, but retains its native template.

### Navigation
The left rail has an MVS heading, a navigation label and 14-DIP navigation items with 12-by-8-DIP padding. Navigation surfaces are at least 42 DIP high, with 6-DIP gaps. Labels and compact icons are white; selection uses a muted green background and semibold header text. Unselected items lighten on hover, while selected background takes precedence over hover. Keyboard focus adds a mint border. Labels and heading collapse with the existing menu state. Subtabs use an alternate surface, with white selected background and a dark-green 2-DIP bottom border.

### Wizard and data grids
Wizard steps use alternate-surface upcoming, dark current, and mint-tinted completed states; Projects shows five equal steps. Grids use white backgrounds, blue-grey bold headers, horizontal separators, alternating cool rows, mint-tinted selection, and alternate-surface hover. Project list rows are 36 DIP high with vertically centered text and status. Sensor-specific row styles additionally apply green, amber, or red backgrounds.

### Toggle
The shared switch is 44 by 24 DIP with an 18-DIP white thumb. Its checked track is mint; hover changes the border, keyboard focus thickens it, and disabled opacity is 0.5.

## Do's and Don'ts

### Do:
- **Do** use the shared SES resources for established control colors and states.
- **Do** retain text labels alongside the existing status and chart color cues.
- **Do** keep data-oriented layouts compact and preserve existing actions and content.

### Don't:
- **Don't** treat a chart series color as an error or success status.
- **Don't** assume every screen uses the shared named text styles or a custom control template.

# Quick Reference: CSS Grid Layout Classes

## Layout Structure Classes

### Main Container
- `.app-shell` - The CSS Grid container wrapping everything

### Grid Areas
- `.app-header` - Fixed height header (80px)
- `.app-sidebar` - Fixed width sidebar (260px desktop, full width mobile)
- `.app-main` - Flexible main content area (houses @RenderBody())
- `.app-footer` - Fixed height footer (60px)

## Header Classes

### Structure
- `.header-content` - Flexbox wrapper for header content
- `.header-logo` - Logo container
- `.logo-img` - Actual logo image
- `.header-user` - User info and menu container

### User Menu
- `.user-menu` - Menu wrapper
- `.user-menu-toggle` - Burger button
- `.burger-line` - Individual burger lines (3 spans)
- `.user-dropdown` - Dropdown menu container
- `.dropdown-item` - Individual dropdown links
- `.user-email` - User email display

## Sidebar Classes

### Navigation
- `.sidebar-nav` - Navigation container (flexbox column)
- `.sidebar-link` - Individual navigation links
- `.sidebar-link.active` - Active/current page link
- `.link-icon` - Icon container (emoji or SVG)
- `.link-text` - Link text content

## Footer Classes

- `.footer-content` - Footer content wrapper

## Utility Classes (from site.css)

- `.text-center` - Center align text
- `.text-muted` - Muted/gray text color
- `.page` - Full height page wrapper
- `.card` - Centered card container (max-width: 800px)

## State Classes

### Interactive States
- `:hover` - Applied on hover (most elements)
- `.active` - Active navigation link
- `[aria-hidden="false"]` - Visible dropdown
- `[aria-expanded="true"]` - Open menu toggle

## CSS Custom Properties

```css
--header-height: 80px
--sidebar-width: 260px
--footer-height: 60px
--shell-gap: 16px
--shell-padding: 18px
--border-radius: 14px
--border-color: rgba(0, 0, 0, 0.12)
--bg-color: #fff
--text-color: #111
--hover-bg: rgba(0, 0, 0, 0.04)
--active-bg: rgba(0, 0, 0, 0.08)
```

## Responsive Breakpoints

- **Desktop**: 1025px and up (full grid)
- **Tablet**: 769px - 1024px (narrower sidebar)
- **Mobile**: 481px - 768px (stacked, horizontal sidebar)
- **Small**: 360px - 480px (full width sidebar links)
- **XSmall**: Below 360px (minimal spacing)

## Common Patterns

### Adding a New Sidebar Link
```html
<a class="sidebar-link @Active("ControllerName")" 
   asp-controller="ControllerName" 
   asp-action="Index">
    <span class="link-icon">??</span>
    <span class="link-text">Link Text</span>
</a>
```

### Adding a Dropdown Item
```html
<a class="dropdown-item" 
   asp-controller="ControllerName" 
   asp-action="Index" 
   role="menuitem">
    Item Text
</a>
```

### Main Content Structure
```html
<main class="app-main" role="main">
    @RenderBody() <!-- Your page content goes here -->
</main>
```

## JavaScript Events

### User Menu Toggle
- Click toggle button ? opens/closes dropdown
- Click outside ? closes dropdown
- Press Escape ? closes dropdown
- Click dropdown item ? closes dropdown and navigates
- Window resize ? closes dropdown

## Grid Template Areas

### Desktop Layout
```css
grid-template-areas:
    "header header"
    "sidebar main"
    "footer footer";
```

### Mobile Layout
```css
grid-template-areas:
    "header"
    "sidebar"
    "main"
    "footer";
```
